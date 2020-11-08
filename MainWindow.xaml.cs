using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Google.Cloud.Language.V1;
using Google.Cloud.Speech.V1;
using System.IO;

using NAudio.Wave;
//using Microsoft.Win32;
//using System.Windows.Forms;
using System.Security;
//using System.Windows.Forms;

namespace Sentimental
{
    public partial class MainWindow : Window
    {
        public BufferedWaveProvider bwp;
        WaveIn waveIn;
        WaveOut waveOut;
        WaveFileWriter writer;
        WaveFileReader reader;
        string audioOutput = "audio.raw";
        string selectedFileName = "";

        LanguageServiceClientBuilder builder;
        LanguageServiceClient client;
        public MainWindow()
        {
            InitializeComponent();
            builder = new LanguageServiceClientBuilder
            {
                CredentialsPath = @"C:\Users\vdmil\Downloads\my_key.json"
            };
            client = builder.Build();

            waveOut = new WaveOut();
            waveIn = new WaveIn();
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
            bwp = new BufferedWaveProvider(waveIn.WaveFormat);
            bwp.DiscardOnBufferOverflow = true;


        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        private void recordAudio_MouseEnter(object sender, MouseEventArgs e)
        {
            recordTB.Visibility = Visibility.Visible;
        }

        private void recordAudio_MouseLeave(object sender, MouseEventArgs e)
        {
            recordTB.Visibility = Visibility.Hidden;
        }

        private void recordAudio_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (NAudio.Wave.WaveIn.DeviceCount < 1)
            {
                System.Windows.MessageBox.Show("No Microphone Detected :(");
                return;
            }
            waveIn.StartRecording();
            recordAudio.Visibility = Visibility.Hidden;
            stopRecordAudio.Visibility = Visibility.Visible;
        }

        private void stopRecordAudio_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            waveIn.StopRecording();
            stopRecordAudio.Visibility = Visibility.Hidden;
            recordAudio.Visibility = Visibility.Visible;
            if (File.Exists(audioOutput))
                File.Delete(audioOutput);

            writer = new WaveFileWriter(audioOutput, waveIn.WaveFormat);
            byte[] buffer = new byte[bwp.BufferLength];
            int offset = 0;
            int count = bwp.BufferLength;

            var read = bwp.Read(buffer, offset, count);
            if (count > 0)
            {
                writer.Write(buffer, offset, read);
            }

            waveIn.Dispose();
            waveIn = new WaveIn();
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
            writer.Close();
            writer = null;


            if (File.Exists(audioOutput))
            {

                SpeechClientBuilder builder = new SpeechClientBuilder
                {
                    CredentialsPath = @"C:\Users\vdmil\Downloads\my_key.json"
                };
                SpeechClient speech = builder.Build();
                var response = speech.Recognize(new RecognitionConfig()
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                    SampleRateHertz = 16000,
                    LanguageCode = "en",
                }, RecognitionAudio.FromFile(audioOutput));


                textEntry.Document.Blocks.Clear();

                string tempStr = "No data available";

                foreach (var result in response.Results)
                {
                    foreach (var alternative in result.Alternatives)
                    {
                        tempStr = alternative.Transcript;
                    }
                }

                textEntry.Document.Blocks.Add(new Paragraph(new Run(tempStr)));

            }
            else
            {
                textEntry.Document.Blocks.Add(new Paragraph(new Run("Audio File Missing")));

            }
        }

        private void analyzeTextBtn_Click(object sender, RoutedEventArgs e)
        {
            TextRange textRange = new TextRange(textEntry.Document.ContentStart, textEntry.Document.ContentEnd);
            string textInEntry = textRange.Text;

            scoreTB.Text = "";
            magnitudeTB.Text = "";
            categoryTB.Text = "";
            syntaxTB.Text = "";
            entitiesTB.Text = "";
            entitySentimentTB.Text = "";

            try
            {
                var sentimentResponse = client.AnalyzeSentiment(new Document()
                {
                    Content = textInEntry,
                    Type = Document.Types.Type.PlainText
                });
                var sentiment = sentimentResponse.DocumentSentiment;
                scoreTB.Text = sentiment.Score.ToString();
                magnitudeTB.Text = sentiment.Magnitude.ToString();
            }
            catch
            {
                scoreTB.Text = "Could not determine a sentiment score";
                magnitudeTB.Text = "Could not determine a magnitude score";
            }

            try
            {
                var classifyResponse = client.ClassifyText(new Document()
                {
                    Content = textInEntry,
                    Type = Document.Types.Type.PlainText
                });
                var classification = classifyResponse.Categories;
                for (int i = 0; i < classification.Count; i++)
                {
                    categoryTB.Text += classification.ElementAt(i) + "\n";
                }
            }
            catch
            {
                categoryTB.Text = "Could not determine a category classification";
            }

            try
            {
                var syntaxResponse = client.AnalyzeSyntax(new Document()
                {
                    Content = textInEntry,
                    Type = Document.Types.Type.PlainText
                });
                var syntax = syntaxResponse.Tokens;
                for (int i = 0; i < syntax.Count; i++)
                {
                    syntaxTB.Text += syntax.ElementAt(i) + "\n";
                }
            }
            catch
            {
                syntaxTB.Text = "Could not determine the syntax";
            }

            try
            {
                var entitiesResponse = client.AnalyzeEntities(new Document()
                {
                    Content = textInEntry,
                    Type = Document.Types.Type.PlainText
                });
                var entities = entitiesResponse.Entities;
                for (int i = 0; i < entities.Count; i++)
                {
                    entitiesTB.Text += entities.ElementAt(i) + "\n";
                }
            }
            catch
            {
                entitiesTB.Text = "Could not determine the entities";
            }

            try
            {
                var entitySentimentResponse = client.AnalyzeEntitySentiment(new Document()
                {
                    Content = textInEntry,
                    Type = Document.Types.Type.PlainText
                });
                var entitySentiment = entitySentimentResponse.Entities;
                for (int i = 0; i < entitySentiment.Count; i++)
                {
                    entitySentimentTB.Text += entitySentiment.ElementAt(i);
                }
            }
            catch
            {
                entitySentimentTB.Text = "Could not determine the entity sentiment";
            }

        }

        private void openTextBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog fileDiag = new System.Windows.Forms.OpenFileDialog();
           // fileDiag.Filter = "Text files (*txt)|*.txt";
            if (fileDiag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedFileName = fileDiag.FileName;
            }
            if (selectedFileName != "")
            {
                string fileTextStr = File.ReadAllText(selectedFileName);
                textEntry.Document.Blocks.Clear();
                textEntry.Document.Blocks.Add(new Paragraph(new Run(fileTextStr)));
            }
            selectedFileName = "";
            
        }

        private void openAudioBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog fileDiag = new System.Windows.Forms.OpenFileDialog();
            // fileDiag.Filter = "Text files (*txt)|*.txt";
            if (fileDiag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedFileName = fileDiag.FileName;
            }
            if (selectedFileName != "")
            {
                SpeechClientBuilder builder = new SpeechClientBuilder
                {
                    CredentialsPath = @"C:\Users\vdmil\Downloads\my_key.json"
                };
                SpeechClient speech = builder.Build();
                var response = speech.Recognize(new RecognitionConfig()
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.EncodingUnspecified,
                    SampleRateHertz = 16000,
                    LanguageCode = "en",
                }, RecognitionAudio.FromFile(selectedFileName));


                textEntry.Document.Blocks.Clear();

                string tempStr = "";

                foreach (var result in response.Results)
                {
                    foreach (var alternative in result.Alternatives)
                    {
                        tempStr += alternative.Transcript;
                    }
                }

                textEntry.Document.Blocks.Add(new Paragraph(new Run(tempStr)));
            }

            selectedFileName = "";
        }
    }
}
