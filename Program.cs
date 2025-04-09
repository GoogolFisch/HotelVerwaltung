using MySql.Data.MySqlClient;
using LightHTTP;
using System.Text;
using System.ComponentModel;
// See https://aka.ms/new-console-template for more information
public class Program{
	public const string sqlServer = @"server=localhost;userid=hotelServer;password=1234;database=hotelServer";
	public static LightHttpServer server = new LightHttpServer(); // https://github.com/javidsho/LightHTTP.git
	public static bool keepAlive = true;
	public static string ConcatAllTypes(object obj){
		string concar = $"{obj}\n---------------\n";
		foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
		{
			string name = descriptor.Name;
			object value = descriptor.GetValue(obj);
			concar += $"{name}\t=\t{value}\n";
		}
		concar += "---------------\n";
		
		Type type = obj.GetType();
		foreach (var method in type.GetMethods())
		{
			var parameters = method.GetParameters();
			var parameterDescriptions = string.Join
			(", ", method.GetParameters()
			.Select(x => x.ParameterType + " " + x.Name)
			.ToArray());

			//Console.WriteLine("{0} {1} ({2})",
			//method.ReturnType,
			//method.Name,
			//parameterDescriptions);
			concar += $"{method.ReturnType}\t{method.Name}\t{parameterDescriptions}\n";
		}
		return concar;
	}
	public static void Main(){
		// MySql connection
		var con = new MySqlConnection(sqlServer);
		con.Open();
		Console.WriteLine($"MySQL version : {con.ServerVersion}");
		// webserver
		// let's find an available port on the local machine, which is useful for unit tests
		var testUrl = server.AddAvailableLocalPrefix();
		Console.WriteLine(testUrl);
		// register our routes and handlers
		server.HandlesPath("/health", context => context.Response.StatusCode = 200);
		server.HandlesPath("/print", async (context, cancellationToken) => {
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/plain";
			context.Request.GetClientCertificate();
			var bytes = Encoding.UTF8.GetBytes(ConcatAllTypes(context.Request));
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		server.HandlesPath("/status", async (context, cancellationToken) => {
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/html";
			var bytes = Encoding.UTF8.GetBytes($"<html><body>"+
					"MySQL version : {con.ServerVersion}<br>"+
					"Current status : Running<br><br>"+
					"<a href=\"/stop\">stopper</a><br>"+
					"<a href=\"https://github.com/javidsho/LightHTTP\">LightHttp</a></body></html>");
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		server.HandlesPath("/stop", async (context, cancellationToken) => {
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/plain";
			var bytes = Encoding.UTF8.GetBytes($"Stopping!");
			keepAlive = false;
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		server.HandlesStaticFile("/main.css", "web-files/main.css");
		server.HandlesStaticFile("/", "web-files/index.html");

		// finally start serving
		server.Start();

		// and call server.Dispose() when done
		while(keepAlive){
			try{
				Thread.Sleep(100);
			}
			catch (OperationCanceledException)
			{
				Console.WriteLine("\nCtrl-C pressed!");
				keepAlive = false;
			}
		}
		// kill Webserver
		server.Dispose();
		// kill MySql
		con.Close();
	}

}
