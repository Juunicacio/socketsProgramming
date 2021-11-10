using System;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    // first goal is to just get it up and running, get a connection, send some data, receive that data back
    // and echo it to the screen.
    class Program
    {
        static void Main(string[] args)
        {
            // we wanna make sure our echo server is running first
            Console.WriteLine("Press Enter to Connect");
            Console.ReadLine();
            
            
            // the address is IPAddress.local or the Loopback. And the port is 9000
            // In our EchoServer we need to star this
            // So just like the server, the client will have an endpoint, which represents where we're going to connect to,
            // whereas on the server, this is where it was going to listen from.
            var endpoint = new IPEndPoint(IPAddress.Loopback, 9000);

            // we'll create a socket
            // Stream just means where the data is just streaming back and forward, it's not packed in any particular manner
            var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // next thing is connect to our endpoint
            socket.Connect(endpoint);

            // and now we can simply read and write
            // true is to make it the owner of that socket
            var networkStream = new NetworkStream(socket, true);
            // the networkStream just makes it easier to read and write data to it

            var msg = "Hello World!";

            // if we take a look at networkStream.Write, you can see that it doesn't take a string, so I can create a string, a string writer 
            // and write to the string. There is a number of ways I can do this, a good fashion is:
            var buffer = System.Text.Encoding.UTF8.GetBytes(msg);

            // the first thing is: how do you represent data going across in your particular system? And a lot of that is 
            // going to come down to the type of message format that you're gonna use, whether it's a binary protocol
            // whether you're sending JSON or XML, how do you move that data back and forward. And depending on the application
            // that you're building, really is going to dictate the protocol.
            // We're going to end up doing XML and there's a lot of reasons for XML and the flexibility behind it, but even
            // with XML, or JSON, when you serialize that down, you get essentially a utf-8 string. And we're going to take that
            // utf-8 string, we get the bytes for it and the reason for doing utf-8 is to make sure that we can properly encode all
            // characters. Obviously English fits in the actual ASCII table chart, but if you need accent marks and foreign language,
            // you're gonna need utf-8 so that you can potentially get to byte Unicode characters to make sure you fully represent
            // everything. So, just start with utf-8, it covers pretty much every case you need when you're doing sort of string based 
            // encoding for messages, if you're doing a binary serialization format that'll be something a little bit different.

            // for right now, we're going to do utf-8, we're gonna get the bytes and now, I'm gonna to write my buffer
            networkStream.Write(buffer, 0, buffer.Length);
            // that just sends it across the stream

            // now I just want to read it back
            // essentially, wat data did I send out and how can I read it back

            // technically we know how big the buffer is that we sent, but for the sake of argument, say we don't know. 
            var response = new byte[1024];

            // We're just going to read back a single buffer and then just display that on the screen
            var bytesRead = networkStream.Read(response, 0, response.Length);
            // In our case, a single read will actually pull back everything we need
            var responseStr = System.Text.Encoding.UTF8.GetString(response);

            Console.WriteLine($"Received : {responseStr}");

            Console.ReadLine();
        }
    }
}
