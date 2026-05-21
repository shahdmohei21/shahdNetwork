using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace client
{
    public partial class Form1 : Form
    {
        private string selectedFile = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    selectedFile = ofd.FileName;
                    Log("File selected");
                }

                if (string.IsNullOrEmpty(selectedFile))
                {
                    Log("No file selected");
                    return;
                }

                Log("Connecting...");

                byte[] fileData = File.ReadAllBytes(selectedFile);

                TcpClient client = new TcpClient();
                client.Connect(IPAddress.Loopback, 5000);

                Log("Connected");

                NetworkStream stream = client.GetStream();

                // send size
                byte[] size = BitConverter.GetBytes(fileData.Length);
                stream.Write(size, 0, 4);

                stream.Write(fileData, 0, fileData.Length);

                Log($"Sending file: {fileData.Length} bytes");

                byte[] compSizeBuffer = new byte[4];
                ReadFull(stream, compSizeBuffer, 0, 4);

                int compSize = BitConverter.ToInt32(compSizeBuffer, 0);

                Log($"Receiving file: {compSize} bytes");


                byte[] compressedData = new byte[compSize];
                ReadFull(stream, compressedData, 0, compSize);

                string output = Path.Combine(
                    Path.GetDirectoryName(selectedFile),
                    "compressed.gz"
                );

                File.WriteAllBytes(output, compressedData);

                Log("File saved");
                Log("Done");

                stream.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Log("Error: " + ex.Message);
            }
        }

        private void ReadFull(NetworkStream stream, byte[] buffer, int offset, int size)
        {
            int total = 0;

            while (total < size)
            {
                int read = stream.Read(buffer, offset + total, size - total);

                if (read == 0)
                    throw new Exception("Connection closed");

                total += read;
            }
        }

        private void Log(string message)
        {
            richTextBox1.AppendText(message + "\n");
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }
    }
}