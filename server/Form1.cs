using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace server
{
    public partial class Form1 : Form
    {
        private TcpListener server;
        private bool serverRunning = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (serverRunning)
                {
                    Log("Server already running.");
                    return;
                }

                server = new TcpListener(IPAddress.Any, 5000);
                server.Start();

                serverRunning = true;

                Log("Server started.");

                Thread serverThread = new Thread(() =>
                {
                    while (serverRunning)
                    {
                        try
                        {
                            TcpClient client = server.AcceptTcpClient();

                            Log("Client connected.");

                            Thread clientThread =
                                new Thread(() => HandleClient(client));

                            clientThread.IsBackground = true;
                            clientThread.Start();
                        }
                        catch (Exception ex)
                        {
                            Log("Accept error: " + ex.Message);
                        }
                    }
                });

                serverThread.IsBackground = true;
                serverThread.Start();
            }
            catch (Exception ex)
            {
                Log("Server error: " + ex.Message);
            }
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();

                byte[] sizeBuffer = new byte[4];
                ReadFull(stream, sizeBuffer, 0, 4);

                int fileSize = BitConverter.ToInt32(sizeBuffer, 0);
                    
                Log($"Receiving file: {fileSize} bytes");

                byte[] fileData = new byte[fileSize];
                ReadFull(stream, fileData, 0, fileSize);

                Log("File received.");

                byte[] compressedData = CompressData(fileData);

                Log($"Compressed size: {compressedData.Length} bytes");

                byte[] compSize = BitConverter.GetBytes(compressedData.Length);

                stream.Write(compSize, 0, 4);
                stream.Write(compressedData, 0, compressedData.Length);

                Log("Compressed file sent.");

                stream.Close();
                client.Close();

                Log("Client disconnected.");
            }
            catch (Exception ex)
            {
                Log("Client error: " + ex.Message);
            }
        }

        private byte[] CompressData(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzip =
                    new GZipStream(ms, CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }

                return ms.ToArray();
            }
        }

        private void ReadFull(NetworkStream stream, byte[] buffer, int offset, int size)
        {
            int total = 0;

            while (total < size)
            {
                int read = stream.Read(buffer, offset + total, size - total);

                if (read == 0)
                    throw new Exception("Connection closed.");

                total += read;
            }
        }

        private void Log(string message)
        {
            richTextBox1.AppendText(message + "\n");
        }
    }
}