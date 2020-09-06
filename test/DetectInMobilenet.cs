﻿using NumSharp;
using System;
using System.IO;
using Tensorflow;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using static Tensorflow.Binding;

namespace test
{
    public class DetectInMobilenet : SciSharpExample, IExample
    {
        public float MIN_SCORE = 0.6f;

        string modelDir = "ssd_mobilenet_v2_coco_2018_01_28";
        string imageDir = "images";
        string pbFile = "frozen_inference_graph.pb";
        string labelFile = "label_map.pbtxt";
        string picFile = "input.jpg";

        public ExampleConfig InitConfig()
            => Config = new ExampleConfig
            {
                Name = "Object Detection",
                Enabled = true,
                IsImportingGraph = true
            };

        public bool Run()
        {
            PrepareData();

            Predict();

            return true;
        }

        public override Graph ImportGraph()
        {
            var graph = new Graph().as_default();
            graph.Import(Path.Join(modelDir, pbFile));

            return graph;
        }

        public override void Predict()
        {
            var sw = new Stopwatch();
            // read in the input image
            sw.Restart();
            var imgArr = ReadTensorFromImageFile(Path.Join(imageDir, "input.jpg"));
            sw.Stop();
            Console.WriteLine($"ReadTensorFromImageFile in {sw.Elapsed.TotalSeconds}s");
            sw.Restart();
            var graph = Config.IsImportingGraph ? ImportGraph() : BuildGraph();
            sw.Stop();
            Console.WriteLine($"Importing/building graph in {sw.Elapsed.TotalSeconds}s");
            using (var sess = tf.Session(graph))
            {
                Tensor tensorNum = graph.OperationByName("num_detections");
                Tensor tensorBoxes = graph.OperationByName("detection_boxes");
                Tensor tensorScores = graph.OperationByName("detection_scores");
                Tensor tensorClasses = graph.OperationByName("detection_classes");
                Tensor imgTensor = graph.OperationByName("image_tensor");
                Tensor[] outTensorArr = new Tensor[] { tensorNum, tensorBoxes, tensorScores, tensorClasses };
                sw.Restart();
                var results = sess.run(outTensorArr, new FeedItem(imgTensor, imgArr));
                sw.Stop();
                Console.WriteLine($"Inference in {sw.Elapsed.TotalSeconds}s");
                sw.Restart();
                buildOutputImage(results);
                sw.Stop();
                Console.WriteLine($"Building output image in {sw.Elapsed.TotalSeconds}s");
            }
        }
        
        public override void PrepareData()
        {
            // get model file
            string url = "https://github.com/mbondarev779/my_object_detection_model/ssd_mobilenet_v2_coco_2018_01_28.tar.gz";
            Web.Download(url, modelDir, "ssd_mobilenet_v2_coco.tar.gz");

            Compress.ExtractTGZ(Path.Join(modelDir, "ssd_mobilenet_v2_coco.tar.gz"), "./");

            // download sample picture
            url = $"https://github.com/mbondarev779/my_object_detection_model/input.jpg";
            Web.Download(url, imageDir, "input.jpg");

            // download the pbtxt file
            url = $"https://github.com/mbondarev779/my_object_detection_model/label_map.pbtxt";
            Web.Download(url, modelDir, "label_map.pbtxt");
            Console.WriteLine("КАЧАЮ ФАЙЛЫ", Color.Blue);
        }
        
        private NDArray ReadTensorFromImageFile(string file_name)
        {
            var graph = tf.Graph().as_default();

            var file_reader = tf.io.read_file(file_name, "file_reader");
            var decodeJpeg = tf.image.decode_jpeg(file_reader, channels: 3, name: "DecodeJpeg");
            var casted = tf.cast(decodeJpeg, TF_DataType.TF_UINT8);
            var dims_expander = tf.expand_dims(casted, 0);

            using (var sess = tf.Session(graph))
                return sess.run(dims_expander);
        }

        private void buildOutputImage(NDArray[] resultArr)
        {
            // get pbtxt items
            PbtxtItems pbTxtItems = PbtxtParser.ParsePbtxtFile(Path.Join(modelDir, "label_map.pbtxt"));

            // get bitmap
            Bitmap bitmap = new Bitmap(Path.Join(imageDir, "input.jpg"));

            var scores = resultArr[2].AsIterator<float>();
            var boxes = resultArr[1].GetData<float>();
            var id = np.squeeze(resultArr[3]).GetData<float>();
            for (int i = 0; i < scores.size; i++)
            {
                float score = scores.MoveNext();
                if (score > MIN_SCORE)
                {
                    float top = boxes[i * 4] * bitmap.Height;
                    float left = boxes[i * 4 + 1] * bitmap.Width;
                    float bottom = boxes[i * 4 + 2] * bitmap.Height;
                    float right = boxes[i * 4 + 3] * bitmap.Width;

                    Rectangle rect = new Rectangle()
                    {
                        X = (int)left,
                        Y = (int)top,
                        Width = (int)(right - left),
                        Height = (int)(bottom - top)
                    };

                    string name = pbTxtItems.items.Where(w => w.id == id[i]).Select(s => s.display_name).FirstOrDefault();

                    drawObjectOnBitmap(bitmap, rect, score, name);
                }
            }

            string path = Path.Join(imageDir, "output.jpg");
            bitmap.Save(path);
            Console.WriteLine($"Processed image is saved as {path}");
        }

        private void drawObjectOnBitmap(Bitmap bmp, Rectangle rect, float score, string name)
        {
            using (Graphics graphic = Graphics.FromImage(bmp))
            {
                graphic.SmoothingMode = SmoothingMode.AntiAlias;

                using (Pen pen = new Pen(Color.Red, 2))
                {
                    graphic.DrawRectangle(pen, rect);

                    Point p = new Point(rect.Right + 5, rect.Top + 5);
                    string text = string.Format("{0}:{1}%", name, (int)(score * 100));
                    graphic.DrawString(text, new Font("Verdana", 8), Brushes.Red, p);
                }
            }
        }
    }
}
