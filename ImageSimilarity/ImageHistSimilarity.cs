using System;
using System.Collections.Generic;
using System.Linq;
using static ImageSimilarity.ImageOperations;

namespace ImageSimilarity
{
    public struct ChannelStats
    {
        public int[] Hist;
        public int Min;
        public int Max;
        public int Med;
        public double Mean;
        public double StdDev;
    }
    public struct ImageInfo
    {
        public string Path;
        public int Width;
        public int Height;
        public ChannelStats RedStats;
        public ChannelStats GreenStats;
        public ChannelStats BlueStats;
    }

    public struct MatchInfo
    {
        public string MatchedImgPath;
        public double MatchScore;
    }
    public class ImageHistSimilarity
    {
        /// <summary>
        /// Calculate the image stats (Max, Min, Med, Mean, StdDev & Histogram) of each color
        /// </summary>
        /// <param name="imgPath">Image path</param>
        /// <returns>Calculated stats of the given image</returns>
        public static ImageInfo CalculateImageStats(string imgPath)
        {
            RGBPixel[,] imageMatrix = OpenImage(imgPath);
            int height = GetHeight(imageMatrix);
            int width = GetWidth(imageMatrix);

            int[] redHist = new int[256];
            int[] greenHist = new int[256];
            int[] blueHist = new int[256];

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    RGBPixel px = imageMatrix[i, j];
                    redHist[px.red]++;
                    greenHist[px.green]++;
                    blueHist[px.blue]++;
                }
            }
            
            ChannelStats redStats = ComputeChannelStats(redHist);
            ChannelStats greenStats = ComputeChannelStats(greenHist);
            ChannelStats blueStats = ComputeChannelStats(blueHist);


            ImageInfo info = new ImageInfo();
            info.Path = imgPath;
            info.Width = width;
            info.Height = height;
            info.RedStats = redStats;
            info.GreenStats = greenStats;
            info.BlueStats = blueStats;

            return info;
        }
        /// <summary>
        /// Load all target images and calculate their stats
        /// </summary>
        /// <param name="targetPaths">Path of each target image</param>
        /// <returns>Calculated stats of each target image</returns>
        public static ImageInfo[] LoadAllImages(string []targetPaths)
        {
            if (targetPaths == null || targetPaths.Length == 0)
                return new ImageInfo[0];

            ImageInfo[] allInfos = new ImageInfo[targetPaths.Length];

            for (int i = 0; i < targetPaths.Length; i++)
            {
                allInfos[i] = CalculateImageStats(targetPaths[i]);
            }
            return allInfos;
        }

        /// <summary>
        /// Match the given query image with the given target images and return the TOP matches as specified
        /// </summary>
        /// <param name="queryPath">Path of the query image</param>
        /// <param name="targetImgStats">Calculated stats of each target image</param>
        /// <param name="numOfTopMatches">Desired number of TOP matches to be returned</param>
        /// <returns>Top matches (image path & distance score) </returns>
        public static MatchInfo[] FindTopMatches(string queryPath, ImageInfo[] targetImgStats, int numOfTopMatches) 
        {
            ImageInfo queryInfo = CalculateImageStats(queryPath);

            List<MatchInfo> matchList = new List<MatchInfo>(targetImgStats.Length);
            double queryTotal = queryInfo.Width * queryInfo.Height;


            double[] qRed = new double[256];
            double[] qGreen = new double[256];
            double[] qBlue = new double[256];

            for (int i = 0; i < 256; i++)
            {
                qRed[i] = queryInfo.RedStats.Hist[i] / queryTotal;
                qGreen[i] = queryInfo.GreenStats.Hist[i] / queryTotal;
                qBlue[i] = queryInfo.BlueStats.Hist[i] / queryTotal;
            }

            foreach (var target in targetImgStats)
            {
                double targetTotal = target.Width * target.Height;


                double[] tRed = new double[256];
                double[] tGreen = new double[256];
                double[] tBlue = new double[256];

                for (int i = 0; i < 256; i++)
                {
                    tRed[i] = target.RedStats.Hist[i] / targetTotal;
                    tGreen[i] = target.GreenStats.Hist[i] / targetTotal;
                    tBlue[i] = target.BlueStats.Hist[i] / targetTotal;
                }

                double angleR = CosineAngle(qRed, tRed);
                double angleG = CosineAngle(qGreen, tGreen);
                double angleB = CosineAngle(qBlue, tBlue);


                double avgAngleDegrees = (angleR + angleG + angleB) / 3.0;

                MatchInfo mi = new MatchInfo();
                mi.MatchedImgPath = target.Path;
                mi.MatchScore = avgAngleDegrees; 
                matchList.Add(mi);
            }

            matchList.Sort((a, b) => a.MatchScore.CompareTo(b.MatchScore));

            return matchList
                .Take(Math.Min(numOfTopMatches, matchList.Count))
                .ToArray();
        }

        private static ChannelStats ComputeChannelStats(int[] hist)
        {
            ChannelStats stats = new ChannelStats();
            stats.Hist = hist;

            // total number of pixels
            long totalPixels = 0;
            for (int i = 0; i < 256; i++)
                totalPixels += hist[i];

            // Min
            int minVal = 0;
            while (minVal < 256 && hist[minVal] == 0) minVal++;
            stats.Min = minVal < 256 ? minVal : 0;

            // Max
            int maxVal = 255;
            while (maxVal >= 0 && hist[maxVal] == 0) maxVal--;
            stats.Max = maxVal >= 0 ? maxVal : 255;

            // Mean
            double sum = 0.0;
            for (int i = 0; i < 256; i++)
                sum += (double)i * hist[i];
            double mean = (totalPixels > 0) ? sum / totalPixels : 0;
            stats.Mean = mean;

            // Median
            long half = totalPixels / 2;
            long cumulative = 0;
            int medianVal = 0;
            for (int i = 0; i < 256; i++)
            {
                cumulative += hist[i];
                if (cumulative >= half)
                {
                    medianVal = i;
                    break;
                }
            }

            stats.Med = medianVal;

            double varianceSum = 0.0;
            for (int i = 0; i < 256; i++)
            {
                double diff = i - mean;
                varianceSum += diff * diff * hist[i];
            }
            double variance = (totalPixels > 1) ? varianceSum / totalPixels : 0.0;
            stats.StdDev = Math.Sqrt(variance);

            return stats;
        }

        private static double CosineAngle(double[] v1, double[] v2)
        {
            double dot = 0.0, norm1 = 0.0, norm2 = 0.0;
            for (int i = 0; i < 256; i++)
            {
                dot += v1[i] * v2[i];
                norm1 += v1[i] * v1[i];
                norm2 += v2[i] * v2[i];
            }

            if (norm1 < 1e-15 || norm2 < 1e-15)
            {

                if (norm1 < 1e-15 && norm2 < 1e-15) return 0.0;
                return 90.0;
            }

            double cosTheta = dot / (Math.Sqrt(norm1) * Math.Sqrt(norm2));

            if (cosTheta > 1.0) cosTheta = 1.0;
            if (cosTheta < -1.0) cosTheta = -1.0;


            double radians = Math.Acos(cosTheta);
            double degrees = radians * (180.0 / Math.PI);
            return degrees;
        }
    }
}
