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
namespace FACE
{
    public partial class Form1 : Form
    {

        MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_TRIPLEX, 0.6d, 0.6d);
        HaarCascade facerecog;
        Capture camera;
        Image<Bgr, Byte> frame;
        Image<Gray, byte> record;
        Image<Gray, byte> grayface = null;
        Image<Gray, byte> trainedfa = null;
        List<Image<Gray, byte>> trainingima = new List<Image<Gray, byte>>();
        List<string> label = new List<string>();
        List<string> user = new List<string>();
        int count, numbers, whatt;
        string CurrentStudent = "";
        string name, names = null;

        SqlConnection connect = new SqlConnection("Data Source=(localdb)\\localDB1;Initial Catalog=emp;Integrated Security=True");
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

            count = count + 1;
            grayface = camera.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            MCvAvgComp[][] DetectedFaces = grayface.DetectHaarCascade(facerecog, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
            foreach (MCvAvgComp f in DetectedFaces[0])
            {
                trainedfa = frame.Copy(f.rect).Convert<Gray, byte>();
                break;
            }
            trainedfa = record.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            trainingima.Add(trainedfa);

            label.Add(textBox1.Text);

            if (!Directory.Exists("faces")) { Directory.CreateDirectory("faces"); }
            File.WriteAllText($"{Application.StartupPath}/faces/faces.txt", $"{trainingima.ToArray().Length}%");

            for (int bi = 1; bi <= trainingima.ToArray().Length + 1; bi++)
            {
                trainingima.ToArray()[bi - 1].Save($"{Application.StartupPath}/faces/face{bi}.bmp");
                File.AppendAllText($"{Application.StartupPath}/faces/faces.txt", $" {label.ToArray()[bi - 1]}%");

            }
            MessageBox.Show(textBox1.Text + " Added Successfuly");
        }





        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            connect.Open();
            SqlCommand cmd = new SqlCommand("select studentnum from studinf where studentname=@studentname",connect);
            cmd.Parameters.AddWithValue("studentname", textBox1.Text);
            SqlDataReader reader = cmd.ExecuteReader();

            if(reader.Read())
            {

                textBox2.Text = reader["studentnum"].ToString();

            }

            else
            {
                textBox2.Text = "";
            }
            connect.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
           FaceRecog fr = new FaceRecog();
            fr.Show();
            this.Hide();




        }

        public Form1()
        {
            InitializeComponent();
            facerecog = new HaarCascade("haarcascade_frontalface_default.xml");
            
            try
            {
                string labelsinfo = File.ReadAllText(Application.StartupPath + "/faces/faces.txt");
                string[] Labels = labelsinfo.Split('%');
                numbers = Convert.ToInt16(Labels[0]);
                count = numbers;
                string faceloader;


                for(int bi = 1; bi < numbers +1;bi++)
                {
                    faceloader = "face" + bi + ".bmp";
                    trainingima.Add(new Image<Gray, byte>(Application.StartupPath + "/faces/" + faceloader));
                    label.Add(Labels[bi]);

                }


            }catch (Exception e) {


                MessageBox.Show("EMPTY DATABASE");


                 }
        
        }

        private void button1_Click(object sender, EventArgs e)
        {


            camera = new Capture();
            camera.QueryFrame();
            Application.Idle += new EventHandler(FrameProcedure);
        }

        private void FrameProcedure(object sender, EventArgs e)
        {
            label2.Text = "0";
            user.Add("");
            frame = camera.QueryFrame().Resize(320, 240,Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            grayface = frame.Convert<Gray, byte>();



            MCvAvgComp[][] FacesDetectedNow = grayface.DetectHaarCascade(facerecog, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,new Size(20,20));
            
            
            foreach (MCvAvgComp f in FacesDetectedNow[0])
            {

                whatt = whatt + 1;
                record=frame.Copy(f.rect).Convert<Gray, Byte>().Resize(100,100,Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                frame.Draw(f.rect, new Bgr(Color.Red), 2);


                if(trainingima.ToArray().Length != 0)
                {
                    MCvTermCriteria termCriteria = new MCvTermCriteria(count, 0.001);
                    EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainingima.ToArray(),label.ToArray(),1500,ref termCriteria);
                    name = recognizer.Recognize(record);
                    CurrentStudent = name;
                    frame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.LightGreen));

                }

                user[whatt - 1] = name;
                user.Add("");
                label2.Text = FacesDetectedNow[0].Length.ToString();

                if(!string.IsNullOrEmpty(name) && !listBox1.Items.Contains(name)) {

                    listBox1.Items.Add(name);
                
                }
            }
            if (FacesDetectedNow[0].Length > 0)
            {
                string[] nameList = user.ToArray();
                listBox1.SelectedItem = nameList[0];
            }

          


            imageBox1.Image = frame;
            whatt = 0;
            user.Clear();

        }
    }
}
