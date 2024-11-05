﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Data.SqlClient;
using Emgu.CV.CvEnum;
using System.Reflection.Emit;
using System.Diagnostics.Eventing.Reader;

namespace FACE
{
    public partial class MANUAL : Form
    {
        MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_TRIPLEX, 0.6d, 0.6d);
        HaarCascade facerecog;
        Capture camera;
        Image<Bgr, Byte> frame;
        Image<Gray, byte> record, trainedfa = null;
        Image<Gray, byte> grayface = null;

        List<Image<Gray, byte>> trainingima = new List<Image<Gray, byte>>();
        List<string> label = new List<string>();
        List<string> user = new List<string>();
        int count, numbers, whatt;
        string CurrentStudent = "";
        string name, names = null;


        SqlConnection connect = new SqlConnection("Data Source=(localdb)\\localDB1;Initial Catalog=empfi;Integrated Security=True");
        private void MANUAL_Load(object sender, EventArgs e)
        {
            ReloadAttendance();


            camera = new Capture();
            camera.QueryFrame();
            Application.Idle += new EventHandler(framegrab);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
           
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            connect.Open();
            SqlCommand cmd = new SqlCommand("select empname from tblemf where empid=@empid", connect);
            cmd.Parameters.AddWithValue("empid", textBox2.Text);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {

                label2.Text = reader["empname"].ToString();
            }

            else
            {
                label2.Text = "";
            }
            connect.Close();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label3.Text = DateTime.Now.ToString("MMMM dd, yyyy");
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            label4.Text = DateTime.Now.ToString("hh:mm:ss tt");
            ReloadAttendance();
        }

        private void framegrab(object sender, EventArgs e)
        {
            user.Add("");
            frame = camera.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            grayface = frame.Convert<Gray, Byte>();



            MCvAvgComp[][] FacesDetectedNow = grayface.DetectHaarCascade(facerecog, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));


            foreach (MCvAvgComp f in FacesDetectedNow[0])
            {

                whatt = whatt + 1;
                record = frame.Copy(f.rect).Convert<Gray, Byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                frame.Draw(f.rect, new Bgr(Color.Red), 2);
                

                if (trainingima.ToArray().Length != 0)
                {
                    MCvTermCriteria termCrit= new MCvTermCriteria(count, 0.001);
                    EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainingima.ToArray(),label.ToArray(), 1500, ref termCrit);
                    name = recognizer.Recognize(record);
                    CurrentStudent = name;
                    frame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.LightGreen));

                }

                user[whatt - 1] = name;
                user.Add("");
                label1.Text = FacesDetectedNow[0].Length.ToString();

                if (!string.IsNullOrEmpty(name) && !textBox1.Text.Contains(name))
                {
                    textBox1.Text.Contains(name);
                }
            }
            if (FacesDetectedNow[0].Length > 0)
            {
                string[] nameList = user.ToArray();
               textBox1.Text = nameList[0];
            }
            imageBox1.Image = frame;
            whatt = 0;
            user.Clear();



        }

        private void button2_Click(object sender, EventArgs e)
        {
            try { 
            
                
            
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        public MANUAL()
        {
            InitializeComponent();
            facerecog = new HaarCascade("haarcascade_frontalface_default.xml");

            try
            {
                string labelsinfo = File.ReadAllText(Application.StartupPath + "/trainedfaces/faces.txt");
                string[] Labels = labelsinfo.Split('%');
                numbers = Convert.ToInt16(Labels[0]);
                count = numbers;
                string faceloader;


                for (int bi = 1; bi < numbers + 1; bi++)
                {
                    faceloader = "face" + bi + ".bmp";
                    trainingima.Add(new Image<Gray, byte>(Application.StartupPath + "/trainedfaces/" + faceloader));
                    label.Add(Labels[bi]);

                }


            }
            catch (Exception e)
            {


                MessageBox.Show("EMPTY DATABASE");


            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
           using (SqlConnection connect = new SqlConnection("Data Source = (localdb)\\localDB1; Initial Catalog = empfi; Integrated Security = True; Encrypt = False"))
                try
                {
                connect.Open();
                string log;
                count++;
                grayface = camera.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                trainedfa = DetectFace(grayface);


                if (trainedfa == null)
                {
                    MessageBox.Show("No Face was Detected.", "Manual Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                trainingima.Add(trainedfa);

                if (string.IsNullOrEmpty(label2.Text))
                {
                    MessageBox.Show("EmployeeID does not exist.", "Make sure you have an employee profile", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                label.Add(textBox2.Text);
                imageBox2.Image = trainedfa;

                if (!Directory.Exists("trainedfaces")) { Directory.CreateDirectory("trainedfaces"); }
                File.WriteAllText($"{Application.StartupPath}/trainedfaces/faces.txt", $"{trainingima.ToArray().Length}%");
               
                for (int i = 1; i <= trainingima.ToArray().Length; i++)
                {
                    trainingima.ToArray()[i - 1].Save($"{Application.StartupPath}/trainedfaces/face{i}.bmp");
                    File.AppendAllText($"{Application.StartupPath}/trainedfaces/faces.txt", $"{label.ToArray()[i - 1]}%");
                }

                MessageBox.Show($"Employee: {label2.Text} face training successfully", "Registration Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);


                    DataTable dt = new DataTable();

                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM tblemf WHERE empid = @empid", connect))
                    {
                        cmd.Parameters.AddWithValue("@empid", textBox2.Text);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }

                    // Check if employee was found
                    if (dt.Rows.Count > 0)
                    {
                        DataTable dt1 = new DataTable();

                        // Second SQL Command to check attendance status
                        using (SqlCommand cmd1 = new SqlCommand("SELECT * FROM empattend WHERE empid = @empid AND logdate = @logdate AND amstat = @amstat AND pmstat = @pmstat", connect))
                        {
                            cmd1.Parameters.AddWithValue("@empid", textBox2.Text);
                            cmd1.Parameters.AddWithValue("@logdate", label3.Text);
                            cmd1.Parameters.AddWithValue("@amstat", "Time In");
                            cmd1.Parameters.AddWithValue("@pmstat", "Time Out");

                            using (SqlDataAdapter adapter1 = new SqlDataAdapter(cmd1))
                            {
                                adapter1.Fill(dt1);
                            }
                        }

                        // Check if the employee has already timed in and out
                        if (dt1.Rows.Count > 0)
                        {
                            MessageBox.Show("You have already timed in and timed out for today", "Already", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            DataTable dt2 = new DataTable();

                            // Third SQL Command to check if the employee has timed in
                            using (SqlCommand cmd2 = new SqlCommand("SELECT * FROM empattend WHERE empid = @empid AND logdate = @logdate AND amstat = @amstat", connect))
                            {
                                cmd2.Parameters.AddWithValue("@empid", textBox2.Text);
                                cmd2.Parameters.AddWithValue("@logdate", label3.Text);
                                cmd2.Parameters.AddWithValue("@amstat", "Time In");

                                using (SqlDataAdapter adapter2 = new SqlDataAdapter(cmd2))
                                {
                                    adapter2.Fill(dt2);
                                }
                            }

                            // If the employee has timed in, update the record
                            if (dt2.Rows.Count > 0)
                            {
                                using (SqlCommand update = new SqlCommand("UPDATE empattend SET getout = @getout, pmstat = @pmstat, totalhours = DATEDIFF(HOUR, timein, @getout) WHERE empid = @empid AND logdate = @logdate", connect))
                                {
                                    update.Parameters.AddWithValue("@getout", label4.Text);
                                    update.Parameters.AddWithValue("@pmstat", "Time Out");
                                    update.Parameters.AddWithValue("@empid", textBox2.Text);
                                    update.Parameters.AddWithValue("@logdate", label3.Text);
                                    update.ExecuteNonQuery();
                                }
                                MessageBox.Show("Successfully Timed Out", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else // If the employee has not timed in, create a new record
                            {
                                using (SqlCommand create = new SqlCommand("INSERT INTO empattend (empid, logdate, timein, amstat) VALUES (@empid, @logdate, @timein, @amstat)", connect))
                                {
                                    create.Parameters.AddWithValue("@empid", textBox2.Text);
                                    create.Parameters.AddWithValue("@logdate", label3.Text);
                                    create.Parameters.AddWithValue("@timein", label4.Text);
                                    create.Parameters.AddWithValue("@amstat", "Time In");
                                    create.ExecuteNonQuery();
                                }
                                MessageBox.Show("Successfully Timed In", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                    else // If the employee ID was not found
                    {
                        MessageBox.Show("Employee ID not found", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    MessageBox.Show(ex.Message.ToString());
                    MessageBox.Show("Enable the face detection first", "Registration Failed!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }




        }



        private Image<Gray, byte> DetectFace(Image<Gray, byte> gray)
        {
            try
            {
                MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(facerecog, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));

                foreach (MCvAvgComp f in facesDetected[0])
                {
                    trainedfa = frame.Copy(f.rect).Convert<Gray, byte>();
                    break;
                }

                return record.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                return null;
            }
        }


        
        public void ReloadAttendance()
        {
            try
            {
               
                    connect.Open();

                    
                    string query = "SELECT tblemf.empname, empattend.logdate, empattend.timein, empattend.amstat, empattend.getout, empattend.pmstat " +"FROM empattend " +"INNER JOIN tblemf ON empattend.empid = tblemf.empid";

                  
                    SqlCommand cmd = new SqlCommand(query, connect);

                  
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);

                
                    DataTable dataTable = new DataTable();

                   
                    adapter.Fill(dataTable);

                    dataGridView1.DataSource = dataTable;

              
                    connect.Close();
                
            }
            catch (SqlException ex)
            {
                MessageBox.Show("A database error occurred: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }






        }








    }
}
