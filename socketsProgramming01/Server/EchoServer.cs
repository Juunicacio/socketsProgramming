using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    // The EchoServer is important because this allows us to really test out our concepts and ideas
    public class EchoServer
    {
        // when you are building a server you wanna make sure that you're
        // using a port that is currently used by somo other system or program
        // running. We are gonna use port 9000

        // next er're gonna create the socket that we're going to listen on
        // and this is why the sever side is a little different than the client side
        // server - socket that allows me to accept connection and when I accept a
        // connection from that socket, the result is a new socket
        public void Start(int port = 9000)
        {
            // where our server is goin to listen: endPoint. And we're gonna use just the Loopback address
            // only listen at 2700
            var endPoint = new IPEndPoint(IPAddress.Loopback, port); 

            // we're gonna create our socket which uses the address family from our endpoint or a stream TCP
            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // we bind that socket to the endpoint, saying this is what I'm goint to be associated with
            socket.Bind(endPoint);

            // and then listen takes a parameter called backlog
            socket.Listen(128);

            // now .NET for whatever reason, the backlog has a defined constant called 'somaxconn', but
            // essentially it is how many pending connections can be sort of queued up in the system that
            // you'll allow for. And really, what that does is a matter of how fast you can respond to those 
            // connections and process them before they begin to timeout so it is a number that you'll have to
            // fiddle with. The default (backlog number) is 128, and we're going to use it.

            // And then the last thing we're gonna to just launch a task. And that task is gonna be called DoEcho
            _ = Task.Run(() => DoEcho(socket));
        }

        private async Task DoEcho(Socket socket)
        {
            do
            {
                // essentially this is probably the only gnarly piece of code in here
                // and this is really just a rapper extension method that's created to 
                // turn async callback style code into a task and just makes wrapping it
                // up a little easier. That is the only gnarly piece of code you'll see,
                // for most part thing are pretty easy.
                // In this case we're just creating a task Factory and wrapping the BeginAccept
                // and EndAccept of a socket 
                // This initial socket that we create is, again, what we're listing on for new connections
                // the result of that, 'BeginAccept' and 'EndAccept' is a brand new socket which is the 'clientSocket'
                var clientSocket = await Task.Factory.FromAsync(
                    new Func<AsyncCallback, object, IAsyncResult>(socket.BeginAccept),
                    new Func<IAsyncResult, Socket>(socket.EndAccept),
                    null).ConfigureAwait(false);


                Console.WriteLine("ECHO SERVER :: CLIENT CONNECTED");

                // NetworkStream is how we read and write to a socket. It's very efficient, it's used underneath the 
                // TCP client, classes in .NET as well. NetworkStream just makes life much easier
                // create a stream with that socket that I can use  
                using var stream = new NetworkStream(clientSocket, true);
                // we're gonna create a deafult buffer of about 1k, so we can read data into it
                var buffer = new byte[1024];

                do
                {
                    // as we read data in, we write that exact data straight back out, that's the whole point of the echo
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                    // anytime you read from a socket, if it comes back a zero, typically means that the client 
                    // connection is gone, but at the lower level of TCP, we did not get the control code to shut
                    // down the socket gracefully and this is one of the challenges in raw socket programming is the
                    // connection from a client to the server is kind of this virtual path, it's not really this physical 
                    // thing, and unless the client correctly shuts down on its end to send the control signal all the way
                    // across, the server won't even know that the client is not there. The only time you kind of figure
                    // it out is read, sometimes it'll come back at zero, sometimes I won't come back out of this read at all
                    // it's not until I try to wrtie to the socket, until I try to do something with the socket itself, before
                    // I know that it's actually not there.
                    // If I read off this, when I didn't get any bites, more likely that the sockets are gone. 
                    // So we break out of this loop and start again for the next connection.
                    if (bytesRead == 0)
                        break;

                    await stream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                } while (true);
            } while (true);
        }

    }
}
