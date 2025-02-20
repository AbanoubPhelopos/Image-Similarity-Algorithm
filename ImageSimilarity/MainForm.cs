using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static ImageSimilarity.ImageHistSimilarity;

namespace ImageSimilarity
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        #region Global Data
        ImageInfo[] allImgsInfo;
        string curImgPath;
        MatchInfo[] topMatches;
        #endregion

        #region EVENT HANDLERS

        #region Buttons Click
        private void btnLoadImages_Click(object sender, EventArgs e)
        {
            txtNumImgs.Text = string.Empty;
            txtLoadTime.Text = string.Empty;
            lstAllImgs.Items.Clear();
            lstMatchedImgs.Items.Clear();

            allImgsInfo = null;
            curImgPath = string.Empty;
            topMatches = null;

            btnOpen.Enabled = false;
            btnMatchImg.Enabled = false;
            grpAllImgs.Visible = false;
            grpMatchedImgs.Visible = false;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string folderPath;
                    Stopwatch sw = Stopwatch.StartNew();
                    {
                        folderPath = folderBrowserDialog1.SelectedPath;
                        List<string> files = GetAllImageFiles(folderPath);
                        string[] imgPaths = files.ToArray();
                        lstAllImgs.Items.AddRange(imgPaths);
                        allImgsInfo = LoadAllImages(imgPaths);
                    }
                    sw.Stop();

                    txtLoadTime.Text = sw.Elapsed.ToString();
                    txtNumImgs.Text = allImgsInfo.Length.ToString();
                    if (allImgsInfo.Length > 0)
                    {
                        btnOpen.Enabled = true;
                        grpAllImgs.Visible = true;
                    }
                    nudNumOfMatches.Maximum = allImgsInfo.Length;

                    if (File.Exists(folderPath + "\\ImagesInfo.dat"))
                    {
                        Dictionary<string, ImageInfo> loadedImgsInfo = LoadImgsInfo(folderPath + "\\ImagesInfo.dat");
                        int numOfWrong = CompareImgsInfo(allImgsInfo, loadedImgsInfo);
                        if (numOfWrong > 0) 
                        {
                            MessageBox.Show("WRONG OUTPUT in " + numOfWrong + " Cases! check console for details");
                        }
                    }
                    else
                    {
                        SaveImgsInfo(allImgsInfo, folderPath, "ImagesInfo.dat");
                    }
                }
                catch (Exception ex)
                { MessageBox.Show(ex.Message); }
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            txtImgPath.Text = string.Empty;
            txtWidth.Text = string.Empty;
            txtHeight.Text = string.Empty;
            pictureBox1.Image = null;
            btnMatchImg.Enabled = false;

            curImgPath = null;

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                Bitmap bm = new Bitmap(OpenedFilePath);
                pictureBox1.Image = bm;
                txtWidth.Text = bm.Width.ToString();
                txtHeight.Text = bm.Height.ToString();
                txtImgPath.Text = OpenedFilePath;

                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    ImageInfo curImgInfo = CalculateImageStats(OpenedFilePath);
                    sw.Stop();

                    ImageOperations.DrawHistogram(curImgInfo.RedStats.Hist, chrtRed1, Color.Red);
                    ImageOperations.DrawHistogram(curImgInfo.GreenStats.Hist, chrtGreen1, Color.Green);
                    ImageOperations.DrawHistogram(curImgInfo.BlueStats.Hist, chrtBlue1, Color.Blue);

                    curImgPath = OpenedFilePath;

                    btnMatchImg.Enabled = true;
                    pnlHist1.Visible = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void btnMatchImg_Click(object sender, EventArgs e)
        {
            lstMatchedImgs.Items.Clear();
            topMatches = null;
            int numOfNearestMatches = (int)nudNumOfMatches.Value;

            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                {
                    topMatches = FindTopMatches(curImgPath, allImgsInfo, numOfNearestMatches);
                }
                sw.Stop();

                txtMatchTime.Text = sw.Elapsed.ToString();
                foreach (var m in topMatches)
                {
                    lstMatchedImgs.Items.Add(m.MatchedImgPath);
                }

                string filePath = curImgPath.Substring(0, curImgPath.LastIndexOf('.'));
                filePath += "_MatchInfo.dat";
                if (File.Exists(filePath))
                {
                    Dictionary<string, MatchInfo> expectedInfo = LoadMatchesInfo(filePath);
                    int numOfWrong = CompareMatchesInfo(topMatches, expectedInfo);
                    if (numOfWrong > 0)
                    {
                        MessageBox.Show("WRONG OUTPUT in " + numOfWrong + " Cases! check console for details");
                    }
                }
                else
                {
                    //topMatches = FindTopMatches(curImgPath, allImgsInfo, allImgsInfo.Length);
                    //SaveMatchesInfo(topMatches, filePath);
                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message); }
        }
        #endregion

        #region Listboxes Select Changed
        private void lstMatchedImgs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstMatchedImgs.SelectedIndex == -1)
            {
                return;
            }
            string imgPath = lstMatchedImgs.Items[lstMatchedImgs.SelectedIndex].ToString();
            if (topMatches != null)
            {
                txtMatchScore.Text = topMatches[lstMatchedImgs.SelectedIndex].MatchScore.ToString();
            }

            // Load the image
            Bitmap bm = new Bitmap(imgPath);
            pictureBox2.Image = bm;

            try
            {
                ImageInfo imageInfo = CalculateImageStats(imgPath);

                ImageOperations.DrawHistogram(imageInfo.RedStats.Hist, chrtRed2, Color.Red);
                ImageOperations.DrawHistogram(imageInfo.GreenStats.Hist, chrtGreen2, Color.Green);
                ImageOperations.DrawHistogram(imageInfo.BlueStats.Hist, chrtBlue2, Color.Blue);

                pnlHist2.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void lstAllImgs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstAllImgs.SelectedIndex == -1)
            {
                return;
            }
            string imgPath = lstAllImgs.Items[lstAllImgs.SelectedIndex].ToString();
            // Load the image
            Bitmap bm = new Bitmap(imgPath);
            pictureBox2.Image = bm;

            try
            {
                ImageInfo imageInfo = CalculateImageStats(imgPath);

                ImageOperations.DrawHistogram(imageInfo.RedStats.Hist, chrtRed2, Color.Red);
                ImageOperations.DrawHistogram(imageInfo.GreenStats.Hist, chrtGreen2, Color.Green);
                ImageOperations.DrawHistogram(imageInfo.BlueStats.Hist, chrtBlue2, Color.Blue);

                pnlHist2.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Other Handlers for UI
        private void btnOpen_EnabledChanged(object sender, EventArgs e)
        {
            if (!btnOpen.Enabled)
            {
                txtHeight.Text = string.Empty;
                txtWidth.Text = string.Empty;
                txtImgPath.Text = string.Empty;
            }
            grpInfo2.Visible = btnOpen.Enabled;
        }
        private void btnMatchImg_EnabledChanged(object sender, EventArgs e)
        {
            lstMatchedImgs.Items.Clear();
            topMatches = null;
            grpMatchedImgs.Visible = grpInfo3.Visible = btnMatchImg.Enabled;
        }
        private void grpAllImgs_VisibleChanged(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
            pictureBox2.Image = null;
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                pnlHist1.Visible = false;
            }
            else
            {
                pnlHist1.Visible = true;
            }
        }
        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBox2.Image == null)
            {
                pnlHist2.Visible = false;
            }
            else
            {
                pnlHist2.Visible = true;
            }
        }
        #endregion

        #endregion

        #region Helper Functions
        private List<string> GetAllImageFiles(string folderPath)
        {
            // Define common image file extensions
            string[] imageExtensions = new string[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.tiff", "*.ico" };

            List<string> imageFiles = new List<string>();

            foreach (string extension in imageExtensions)
            {
                // Get all files with the current extension in the folder
                string[] files = Directory.GetFiles(folderPath, extension);

                // Add the found files to the imageFiles list
                imageFiles.AddRange(files);
            }

            return imageFiles;
        }

        private void SaveImgsInfo(ImageInfo[] allImgsInfo, string folderPath, string fileName)
        {
            Stream s = new FileStream(folderPath + "\\" + fileName, FileMode.Create);
            var sw = new BinaryWriter(s);
            sw.Write(allImgsInfo.Length);
            foreach (var img in allImgsInfo)
            {
                sw.Write(img.Path);
                sw.Write(img.Width);
                sw.Write(img.Height);

                //Hists
                for (int i = 0; i < 256; i++)
                {
                    sw.Write(img.RedStats.Hist[i]);
                    sw.Write(img.GreenStats.Hist[i]);
                    sw.Write(img.BlueStats.Hist[i]);
                }
                //Others
                sw.Write(img.RedStats.Min); sw.Write(img.RedStats.Max); sw.Write(img.RedStats.Med); sw.Write(img.RedStats.Mean); sw.Write(img.RedStats.StdDev);
                sw.Write(img.GreenStats.Min); sw.Write(img.GreenStats.Max); sw.Write(img.GreenStats.Med); sw.Write(img.GreenStats.Mean); sw.Write(img.GreenStats.StdDev);
                sw.Write(img.BlueStats.Min); sw.Write(img.BlueStats.Max); sw.Write(img.BlueStats.Med); sw.Write(img.BlueStats.Mean); sw.Write(img.BlueStats.StdDev);
            }

            s.Close();
            sw.Close();
        }

        private Dictionary<string, ImageInfo> LoadImgsInfo(string filePath)
        {
            Stream s = new FileStream(filePath, FileMode.Open);
            var sr = new BinaryReader(s);
            int numOfImgs = sr.ReadInt32();
            Dictionary<string, ImageInfo> imgsInfo = new Dictionary<string, ImageInfo>(numOfImgs);

            for (int m = 0; m < numOfImgs; m++)
            {
                ImageInfo imageInfo = new ImageInfo();
                imageInfo.Path = sr.ReadString();
                imageInfo.Width = sr.ReadInt32();
                imageInfo.Height = sr.ReadInt32();
                //Hists
                imageInfo.RedStats.Hist = new int[256];
                imageInfo.GreenStats.Hist = new int[256];
                imageInfo.BlueStats.Hist = new int[256];
                for (int i = 0; i < 256; i++)
                {
                    imageInfo.RedStats.Hist[i] = sr.ReadInt32();
                    imageInfo.GreenStats.Hist[i] = sr.ReadInt32();
                    imageInfo.BlueStats.Hist[i] = sr.ReadInt32();

                }
                //Others
                imageInfo.RedStats.Min = sr.ReadInt32();
                imageInfo.RedStats.Max = sr.ReadInt32();
                imageInfo.RedStats.Med = sr.ReadInt32();
                imageInfo.RedStats.Mean = sr.ReadDouble();
                imageInfo.RedStats.StdDev = sr.ReadDouble();

                imageInfo.GreenStats.Min = sr.ReadInt32();
                imageInfo.GreenStats.Max = sr.ReadInt32();
                imageInfo.GreenStats.Med = sr.ReadInt32();
                imageInfo.GreenStats.Mean = sr.ReadDouble();
                imageInfo.GreenStats.StdDev = sr.ReadDouble();

                imageInfo.BlueStats.Min = sr.ReadInt32();
                imageInfo.BlueStats.Max = sr.ReadInt32();
                imageInfo.BlueStats.Med = sr.ReadInt32();
                imageInfo.BlueStats.Mean = sr.ReadDouble();
                imageInfo.BlueStats.StdDev = sr.ReadDouble();

                imgsInfo.Add(imageInfo.Path, imageInfo);
            }

            s.Close();
            sr.Close();

            return imgsInfo;
        }
        private int CompareImgsInfo(ImageInfo[] allImgsInfo, Dictionary<string, ImageInfo> loadedImgsInfo)
        {
            //throw new NotImplementedException();
            string msg = string.Empty;
            int numOfWrong = 0;
            foreach (var imgInf1 in allImgsInfo)
            {
                bool correct = true;

                var imgInf2 = loadedImgsInfo[imgInf1.Path];
                msg += "\n===================================\n" + imgInf1.Path + "\n===================================\n";
                //Dims
                if (imgInf1.Width != imgInf2.Width || imgInf1.Height != imgInf2.Height)
                {
                    msg += "Dim Mismatch!\n";
                    correct = false;
                }

                //Red Stats
                if (imgInf1.RedStats.Min != imgInf2.RedStats.Min)
                {
                    msg += $"Red Max Mismatch! Expected {imgInf1.RedStats.Min} Actual {imgInf2.RedStats.Min}\n";
                    correct = false;
                }
                if (imgInf1.RedStats.Max != imgInf2.RedStats.Max)
                {
                    msg += $"Red Max Mismatch! Expected {imgInf1.RedStats.Max} Actual {imgInf2.RedStats.Max}\n";
                    correct = false;
                }
                if (imgInf1.RedStats.Med != imgInf2.RedStats.Med)
                {
                    msg += $"Red Med Mismatch! Expected {imgInf1.RedStats.Med} Actual {imgInf2.RedStats.Med}\n";
                    correct = false;
                }
                if (Math.Round(imgInf1.RedStats.Mean, 5) != Math.Round(imgInf2.RedStats.Mean, 5))
                {
                    msg += $"Red Mean Mismatch! Expected {Math.Round(imgInf1.RedStats.Mean, 5)} Actual {Math.Round(imgInf2.RedStats.Mean, 5)}\n";
                    correct = false;
                }
                if (Math.Round(imgInf1.RedStats.StdDev, 5) != Math.Round(imgInf2.RedStats.StdDev, 5))
                {
                    msg += $"Red StdDev Mismatch! Expected {Math.Round(imgInf1.RedStats.StdDev, 5)} Actual {Math.Round(imgInf2.RedStats.StdDev, 5)}\n";
                    correct = false;
                }
                for (int i = 0; i < 256; i++)
                {
                    if (imgInf1.RedStats.Hist[i] != imgInf2.RedStats.Hist[i])
                    {
                        msg += "Red Hist Mismatch!\n";
                        correct = false; break;
                    }
                }
                //Green Stats
                if (imgInf1.GreenStats.Min != imgInf2.GreenStats.Min)
                {
                    msg += $"Green Max Mismatch! Expected {imgInf1.GreenStats.Min} Actual {imgInf2.GreenStats.Min}\n";
                    correct = false;
                }
                if (imgInf1.GreenStats.Max != imgInf2.GreenStats.Max)
                {
                    msg += $"Green Max Mismatch! Expected {imgInf1.GreenStats.Max} Actual {imgInf2.GreenStats.Max}\n";
                    correct = false;
                }
                if (imgInf1.GreenStats.Med != imgInf2.GreenStats.Med)
                {
                    msg += $"Green Med Mismatch! Expected {imgInf1.GreenStats.Med} Actual {imgInf2.GreenStats.Med}\n";
                    correct = false;
                }
                if (Math.Round(imgInf1.GreenStats.Mean, 5) != Math.Round(imgInf2.GreenStats.Mean, 5))
                {
                    msg += $"Green Mean Mismatch! Expected {Math.Round(imgInf1.GreenStats.Mean, 5)} Actual {Math.Round(imgInf2.GreenStats.Mean, 5)}\n";
                    correct = false;
                }
                if (Math.Round(imgInf1.GreenStats.StdDev, 5) != Math.Round(imgInf2.GreenStats.StdDev, 5))
                {
                    msg += $"Green StdDev Mismatch! Expected {Math.Round(imgInf1.GreenStats.StdDev, 5)} Actual {Math.Round(imgInf2.GreenStats.StdDev, 5)}\n";
                    correct = false;
                }
                for (int i = 0; i < 256; i++)
                {
                    if (imgInf1.GreenStats.Hist[i] != imgInf2.GreenStats.Hist[i])
                    {
                        msg += "Green Hist Mismatch!\n";
                        correct = false; break;
                    }
                }
                //Blue Stats
                if (imgInf1.BlueStats.Min != imgInf2.BlueStats.Min)
                {
                    msg += $"Blue Max Mismatch! Expected {imgInf1.BlueStats.Min} Actual {imgInf2.BlueStats.Min}\n";
                    correct = false;
                }
                if (imgInf1.BlueStats.Max != imgInf2.BlueStats.Max)
                {
                    msg += $"Blue Max Mismatch! Expected {imgInf1.BlueStats.Max} Actual {imgInf2.BlueStats.Max}\n";
                    correct = false;
                }
                if (imgInf1.BlueStats.Med != imgInf2.BlueStats.Med)
                {
                    msg += $"Blue Med Mismatch! Expected {imgInf1.BlueStats.Med} Actual {imgInf2.BlueStats.Med}\n";
                    correct = false;
                }
                if (Math.Round(imgInf1.BlueStats.Mean, 5) != Math.Round(imgInf2.BlueStats.Mean, 5))
                {
                    msg += $"Blue Mean Mismatch! Expected {Math.Round(imgInf1.BlueStats.Mean, 5)} Actual {Math.Round(imgInf2.BlueStats.Mean, 5)}\n";
                    correct = false;
                }
                if (Math.Round(imgInf1.BlueStats.StdDev, 5) != Math.Round(imgInf2.BlueStats.StdDev, 5))
                {
                    msg += $"Blue StdDev Mismatch! Expected {Math.Round(imgInf1.BlueStats.StdDev, 5)} Actual {Math.Round(imgInf2.BlueStats.StdDev, 5)}\n";
                    correct = false;
                }
                for (int i = 0; i < 256; i++)
                {
                    if (imgInf1.BlueStats.Hist[i] != imgInf2.BlueStats.Hist[i])
                    {
                        msg += "Blue Hist Mismatch!\n";
                        correct = false; break;
                    }
                }

                if (correct)
                {
                    msg += "CORRECT\n";
                }
                else
                {
                    msg += "WRONG\n";
                    numOfWrong++;
                }

            }

            Console.WriteLine(msg);
            return numOfWrong;
        }
        private void SaveMatchesInfo(MatchInfo[] matchInfo, string filePath)
        {
            Stream s = new FileStream(filePath, FileMode.Create);
            var sw = new BinaryWriter(s);
            sw.Write(matchInfo.Length);
            foreach (var match in matchInfo)
            {
                sw.Write(match.MatchedImgPath);
                sw.Write(match.MatchScore);
            }

            s.Close();
            sw.Close();
        }
        private Dictionary<string, MatchInfo> LoadMatchesInfo(string filePath)
        {
            Stream s = new FileStream(filePath, FileMode.Open);
            var sr = new BinaryReader(s);
            int numOfMatches = sr.ReadInt32();
            Dictionary<string, MatchInfo> matchesInfo = new Dictionary<string, MatchInfo>(numOfMatches);

            for (int m = 0; m < numOfMatches; m++)
            {
                MatchInfo matchInfo = new  MatchInfo();
                matchInfo.MatchedImgPath = sr.ReadString();
                matchInfo.MatchScore = sr.ReadDouble();
                matchesInfo.Add(matchInfo.MatchedImgPath, matchInfo);
            }

            s.Close();
            sr.Close();

            return matchesInfo;
        }
        private int CompareMatchesInfo(MatchInfo[] calculatedInfo, Dictionary<string, MatchInfo> expectedInfo)
        {
            //throw new NotImplementedException();
            string msg = string.Empty;
            int numOfWrong = 0;
            foreach (var matchInf1 in calculatedInfo)
            {
                bool correct = true;

                var matchInf2 = expectedInfo[matchInf1.MatchedImgPath];
                msg += "\n===================================\n" + matchInf1.MatchedImgPath + "\n===================================\n";
                //Scores
                if (Math.Round(matchInf1.MatchScore, 5) != Math.Round(matchInf2.MatchScore, 5))
                {
                    msg += "Scores Mismatch!\n";
                    correct = false;
                }
                if (correct)
                {
                    msg += "CORRECT\n";
                }
                else
                {
                    msg += "WRONG\n";
                    numOfWrong++;
                }

            }

            Console.WriteLine(msg);
            return numOfWrong;
        }

        #endregion

    }
}