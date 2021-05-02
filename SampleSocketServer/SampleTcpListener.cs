using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SampleSocketServer
{
    class SampleTcpListener
    {
        public static readonly string Local_IP = "127.0.0.1";
        public static readonly Int32 Common_Port = 10012;

        public enum Protocol
        {
            req_quiz_question = 1000,
            req_quiz_answer,

            res_quiz_question = 2000,
            res_quiz_answer,
        }

        ConcurrentBag<TcpClient> client_set_;

        TcpListener tcp_listener_;
        IPAddress local_addr_;
        Int32 port_;

        int user_counter_;

        int right_answer_index_;
        int wait_counter_for_answer_clinet_;

        Random random;

        StringBuilder sb;

        public SampleTcpListener(string ip, int port)
        {
            local_addr_ = IPAddress.Parse(ip);
            port_ = port;
            client_set_ = new ConcurrentBag<TcpClient>();
            user_counter_ = 0;

            sb = new StringBuilder();

            random = new Random((int)DateTime.Now.Ticks);

            int rand_index = random.Next(SampleQuiz.Quiz_Questions.GetLength(0));
            string word = SampleQuiz.Quiz_Questions[rand_index, 0];
            string mean = SampleQuiz.Quiz_Questions[rand_index, 1];
            Console.Out.WriteLine("Test Print : word = {0}, mean = {1}", word, mean);

            Console.Out.WriteLine("Test Print : Protocol = {0}, {1}", Protocol.req_quiz_question, Protocol.req_quiz_question.GetHashCode());
        }

        public void startListen()
        {
            Console.Out.WriteLine("Start Server & Listen!, Port = {0}", port_);

            try
            {
                tcp_listener_ = new TcpListener(local_addr_, port_);
                tcp_listener_.Start();

                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = tcp_listener_.AcceptTcpClient();
                    Console.WriteLine("#" + user_counter_ + " Connected!");

                    client_set_.Add(client);

                    ConnectionHandler connection_handler = new ConnectionHandler();
                    connection_handler.client_ = client;
                    connection_handler.user_id_ = user_counter_;
                    connection_handler.client_set_ = client_set_;
                    connection_handler.sample_listener_ = this;

                    client = null;

                    Thread new_handler = new Thread(new ThreadStart(connection_handler.clientHandler));
                    new_handler.Start();

                    ++user_counter_;
                }

            }
            catch (SocketException e) { Console.WriteLine("SocketException: {0}", e); }
            finally { tcp_listener_.Stop(); tcp_listener_ = null; }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        public void receiveByClient(string message, int user_id)
        {
            lock (this)
            {
                Console.WriteLine("Sent: {0}, Client ID: {1}", message, user_id);

                sb.Length = 0;
                string[] param = message.Split('&');

                switch ((Protocol)System.Convert.ToInt32(param[0]))
                {
                    case Protocol.req_quiz_question:
                        int quiz_length = SampleQuiz.Quiz_Questions.GetLength(0);
                        int rand_index = random.Next(quiz_length);
                        string word = SampleQuiz.Quiz_Questions[rand_index, 0];

                        int right_mean_array_index = random.Next(4);
                        //string mean = SampleQuiz.Quiz_Questions[rand_index, 1];

                        right_answer_index_ = rand_index;

                        sb.AppendFormat("{0}&{1}", Protocol.res_quiz_question.GetHashCode(), word);

                        for (int i = 0; i < 4; ++i)
                        {
                            if (i == right_mean_array_index)
                            {
                                sb.AppendFormat("&{0}", SampleQuiz.Quiz_Questions[right_answer_index_, 1]);
                            }
                            else
                            {
                                rand_index = random.Next(quiz_length);
                                sb.AppendFormat("&{0}", SampleQuiz.Quiz_Questions[rand_index, 1]);
                            }
                        }
                        break;
                    case Protocol.req_quiz_answer:

                        break;
                }

                broadCast(sb.ToString());
            }
        }

        public void broadCast(string message)
        {
            Console.Out.WriteLine("bradCast message = " + message);

            byte[] msg = //System.Text.Encoding.UTF8.GetBytes(message);
                System.Text.Encoding.ASCII.GetBytes(message);
            byte[] legth = { (byte)msg.Length };
            try
            {
                foreach (TcpClient tcp_client in client_set_)
                {
                    Console.WriteLine("data length = {0}", msg.Length);

                    NetworkStream stream = tcp_client.GetStream();
                    stream.Write(legth, 0, legth.Length);
                    stream.Write(msg, 0, msg.Length);
                }
            }
            catch (Exception e) { Console.WriteLine("Exception: {0}", e); }
        }
        static void Main(string[] args)
        {
            new SampleTcpListener(Local_IP, Common_Port).startListen();
        }
    }
}
