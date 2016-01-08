public class MySocket
{
    public object tcpClient;

    public MySocket()
    {
        //System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient();
        //UnityEngine.Debug.Log(client.GetType().Assembly.GetName().Name);

        //tcpClient = client;//System.Activator.CreateInstance(GetType().Assembly.FullName, "System.Net.Sockets.TcpClient");

        System.Type testType = typeof(System.Uri);
        System.Type clazz = testType.Assembly.GetType("System.Net.Sockets.TcpClient");
        tcpClient = clazz.GetConstructor(new System.Type[]{}).Invoke(null);

        //tcpClient = System.Activator.CreateInstance(testType.Assembly.FullName, "System.Net.Sockets.TcpClient");

        availableProperty = clazz.GetProperty("Available");
        connectedProperty = clazz.GetProperty("Connected");

        connectMethod = clazz.GetMethod("Connect", new System.Type[] {typeof(string), typeof(int)});
        getStreamMethod = clazz.GetMethod("GetStream");
        closeMethod = clazz.GetMethod("Close");
    }

    private System.Reflection.MethodInfo connectMethod;
    private System.Reflection.MethodInfo getStreamMethod;
    private System.Reflection.MethodInfo closeMethod;
    
    private System.Reflection.PropertyInfo connectedProperty;
    private System.Reflection.PropertyInfo availableProperty;

    public void Connect(string address, int port)
    {
        connectMethod.Invoke(tcpClient, new object[] { address, port });
    }

    public bool Connected
    {
        get
        {
            return (System.Boolean)connectedProperty.GetValue(tcpClient, null);
        }
    }

    public System.IO.Stream GetStream()
    {
        return (System.IO.Stream)getStreamMethod.Invoke(tcpClient, null);
    }

    public int Available
    {
        get
        {
            return (System.Int32)availableProperty.GetValue(tcpClient, null);
        }
    }

    public void Close()
    {
        closeMethod.Invoke(tcpClient, null);
    }
}