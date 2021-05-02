using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace SampleSocketServer
{
    class ConnectionHandler
    {
        public int user_id_;

        public TcpClient client_;

        public ConcurrentBag<TcpClient> client_set_;

        public SampleTcpListener sample_listener_;
        NetworkStream stream_;
        public void clientHandler()
        {
            int i;

            Byte[] bytes = new Byte[256];
            String data = null;

            try {

                stream_ = client_.GetStream();

                // Loop to receive all the data sent by the client.
                while ((i = stream_.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("Received: {0}", data + user_id_);

                    // Process the data sent by the client.
                    data = data.ToUpper();

                    sample_listener_.receiveByClient(data, user_id_);
                }
            }
            catch (SocketException e) { Console.WriteLine("SocketException: {0}", e); }
            catch (Exception e) { Console.WriteLine("Exception: {0}", e); }
            finally {
                stream_.Close();
                client_set_.TryTake(out client_);
                client_.Close();
                client_ = null;
            }
        }
    }
}
