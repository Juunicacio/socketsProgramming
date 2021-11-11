using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Client
{
    public class MyMessage
    {
        // this is the message we're gonna want to send back and forth
        // and again, to encode this message, we need to pick a format binary (JSON, XML or some proprietary one)
        // XML is extremely flexible encoding, there's a ton of tools for it, being able to translate and transform it with XSLT
        // as part of a processing pipeline and mapping, it's one of the most heavily used message integrations format
        // that's out there
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
    }
    class Program
    {
        // we wanna be able to send the message:
        static async Task SendAsync<T>(NetworkStream networkStream, T message)
        {
            // If I'm gonna send something, I'm going to encode it, 
            // I'll get back a header and a body for the encoding 
            // I can go ahead and write the header then I can go ahead and write the body
            // If I encode the message, than I can do a await
            var (header, body) = Encode(message);
            await networkStream.WriteAsync(header, 0, header.Length).ConfigureAwait(false);
            await networkStream.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
        }
        // the opposite of SendAsync is to receive, so:
        static async Task<T> ReceiveAsync<T>(NetworkStream networkStream)
        {
            // receive is gonna be sort of the opposite of send
            // we know that the header is four bytes, so I can easily say:
            //var header = new byte[4];
            // now I need to be able to read in four bytes, so I'm gonna
            // make a nica little method down beloe called ReadAsync
            var headerBytes = await ReadAsync(networkStream, 4);

            // now I have to turn this header byte package into an integer value that we can use to
            // determine how big the body is I need to read

            // this part we're taking the bytes, turn into an integer but because they're in network order,
            // I now have to go from network order to host order
            // why that's a function on IPAddress? I have no a clue, that just seems like the most bizarre place
            // for that particular function to be, but it is.
            var bodyLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(headerBytes));

            // so it the headerLength I can now say:
            var bodyBytes = await ReadAsync(networkStream, bodyLength);

            // now with the body, I can return
            return Decode<T>(bodyBytes);
        }


        // so, in order for me to send a particular message, in this case my message, 
        // we need to be able to encode the message:
        // byte array to the header and byte array to the body
        static (byte[] header , byte[] body) Encode<T>(T message)
        {
            // Let's inplement our encoder, and again we're just gonna use XML, write it out pretty simple, so:
            var xs = new XmlSerializer(typeof(T));
            // we want to end up writing this to a string, so that we can take the string and get the bytes
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            xs.Serialize(sw, message);

            // now that is completely serialized, just writes the XML into a string writer
            var bodyBytes = Encoding.UTF8.GetBytes(sb.ToString());
            // and now, here's where we need to create the header bytes
            var headerBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bodyBytes.Length));

            return (headerBytes, bodyBytes);

            // overral: take the message, whatever that object is, serialize it into XML, get the bytes from that
            // utf-8 encoded, get the length and then pull that back out

        }
        // we want to decode
        static T Decode<T>(byte[] body)
        {
            //  now from the body, we just want to decode these bytes
            var str = Encoding.UTF8.GetString(body);

            // now we need to load up this string into the serializer, because we want to deserialize from this guy
            var sr = new StringReader(str);
            var xs = new XmlSerializer(typeof(T));
            // deserialize string reader that gives me an object, so return it as T
            return (T) xs.Deserialize(sr);
        }

        // little helpful method to read in four bytes. This will just continue to read off the socket until 
        // we have all of the data or the number of bytes we're requesting for it to read
        static async Task<byte[]> ReadAsync(NetworkStream networkStream, int bytesToRead)
        {
            var buffer = new byte[bytesToRead];
            var bytesRead = 0;
            while(bytesRead < bytesToRead)
            {
                // we keep track of how many bytes we've read, that will become the offset into the buffer 
                // and then how many bytes that we want to try to read, we try to read as many as we can
                // this can be problematic because, if you remember, if we get a zero, that means the client is gone
                // so we create a new variable called bytesReceived
                var bytesReceived = await networkStream.ReadAsync(buffer, bytesRead, (bytesToRead - bytesRead)).ConfigureAwait(false);
                // if that new varible is equal to zero, then basically the socket is closed
                if (bytesReceived == 0)
                    throw new Exception("Socket Closed");
                bytesRead += bytesReceived;
            }
            return buffer;
        }

        // Overral for the code above: we're gonna send a message,
        // we're first going to encode it, so serialize the message itself
        // and create the header, which in our case is going to be a four byte for byte length header
        // expressed a network byte order and so this is another gotcha that many programmers miss
        // is when you're sending a numeric representation, which is different than encoding our message and 
        // serializing it, but if I'm sending byte values that specifically represent numeric values and 
        // I'm going to directly interpret those bytes, those bytes need to be a network byte order. And
        // what that allows us to do is be agnostic between different types of systems that have big-endian 
        // or little-endian byte ordering for the way they represent numeric values.
        // let's tie it together



        // first goal is to just get it up and running, get a connection, send some data, receive that data back
        // and echo it to the screen.
        static async Task Main(string[] args)
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


            /*
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
            */

            // new:
            var myMessage = new MyMessage
            {
                IntProperty = 404,
                StringProperty = "I'm here"
            };


            Console.WriteLine("Sending");
            Print(myMessage);

            // since this is an async, we now need the main static function to be an async Task
            // we send our message off
            await SendAsync(networkStream, myMessage).ConfigureAwait(false);

            // now we want to receive
            // the type of the message we wanna receive back from the networkStream
            var responseMsg = await ReceiveAsync<MyMessage>(networkStream).ConfigureAwait(false);

            Console.WriteLine("Received");
            Print(responseMsg);

            Console.ReadLine();

            // how can I send a message, and what does that message look like, 
            // how can I encode it, how can I decode it 
            // we're gonna start sending a message and look at how we can properly encode
            // a message so we know how to send it and more importantly, how to receive it in its entirety
            // see at the top of the page (before starting the class)
        }

        // print function:
        static void Print(MyMessage m) => Console.WriteLine($"MyMessage.IntProperty = {m.IntProperty}, MyMessage.StringProperty = {m.IntProperty}");
    }
}
