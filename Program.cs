using MySql.Data.MySqlClient;
using LightHTTP;
using System.Text;
using System.ComponentModel;
// See https://aka.ms/new-console-template for more information
public class Program{
	/*
	public const string sqlServer = @"server=localhost;userid=hotelServer;password=1234;database=hotelServer";
	public static LightHttpServer server = new LightHttpServer(); // https://github.com/javidsho/LightHTTP.git

	public static MySqlConnection con;*/
	public static DateTime serverStartTime;// = DateTime.Now;
	public static Servicer service;
	public static bool keepAlive = true;
	
	public static string ConcatAllTypes(object obj){
		// name
		string concar = $"{obj}\n---------------\n";
		// class variables
		foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
		{
			string name = descriptor.Name;
			object? value = descriptor.GetValue(obj);
			if(value is null)
				concar += $"{name}\t=\t<null>\n";
			else
				concar += $"{name}\t=\t{value}\n";
		}
		concar += "---------------\n";
		//class methods
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
	public static void Main(string []arguments){
		serverStartTime = DateTime.Now;
		service = new Servicer();

		Console.WriteLine(service.webServerUrl);

		service.EnsureTableFormat("Kunden",""); // idk
		// register everything!
		// register funny logical stuff
		service.server.Handles(str => (str == "/print" || str.StartsWith("/print/")),async (context,cancellationToken) => {
				// print some debug info
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/plain";
			context.Request.GetClientCertificate(); // this has to be done!
			var bytes = Encoding.UTF8.GetBytes(ConcatAllTypes(context.Request));
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		service.server.Handles(str => (str == "/tables" || str.StartsWith("/tables/")),async (context,cancellationToken) => {
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/plain";
			string data;
			string tblName = Servicer.Sanitise(context.Request.RawUrl.Substring(7));
			Console.WriteLine($"<{tblName}>");
			if(tblName == ""){
				data = "Tables:\n";
				foreach(string tbl in service.existingTables){
					data += tbl + "\n";
				}
			}
			else{
				data = $"Table: {tblName}\n";
				var schema = service.con.GetSchema(tblName);
				data += ConcatAllTypes(schema);
				/*foreach (System.Data.DataColumn col in schema.Columns)
				{
					data += $"{col.ColumnName} - ";
				}*/
				data += Servicer.GetTableData(schema);
			}
			byte[] bytes = Encoding.UTF8.GetBytes(data);
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		service.server.HandlesPath("/status", async (context, cancellationToken) => {
				// print current status
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/html";
			//TimeSpan upTime = DateTime.Now - serverStartTime;
			var bytes = Encoding.UTF8.GetBytes($"<html><body>"+
					$"MySQL version : {service.con.ServerVersion}<br>"+
					$"time at request : {DateTime.Now.ToString("HH:mm:ss")}<br>" + 
					//$"up-time : {upTime}s<br>"+
					$"up-time : {DateTime.Now - serverStartTime}s<br>"+
					"Current status : Running<br><br>"+
					// and some links
					"<a href=\"/stop\">Stop Server</a><br>"+
					"<a href=\"https://github.com/javidsho/LightHTTP\">LightHttp</a></body></html>");
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		// register local-files
		service.server.HandlesStaticFile("/main.css", "web-files/main.css");
		service.server.HandlesStaticFile("/", "web-files/index.html");
		service.server.HandlesStaticFile("/food", "web-files/food.html");
		service.server.HandlesStaticFile("/book", "web-files/book.html");
		service.server.HandlesStaticFile("/location", "web-files/location.html");
		service.server.HandlesStaticFile("/contact", "web-files/contact.html");

		//
		service.Start();
		// https://medium.com/@rainer_8955/gracefully-shutdown-c-apps-2e9711215f6d
		Console.CancelKeyPress += (_, ea) =>
		{
			// Tell .NET to not terminate the process imidieatly
			ea.Cancel = true;

			Console.WriteLine("Received SIGINT (Ctrl+C)");
			keepAlive = false;
		};
		//
		while(keepAlive){
			Thread.Sleep(100);
		}
		// stopping
		service.Stop();
	}
	/*
	public static void Main(string []arguments){
		// MySql connection
		con = new MySqlConnection(sqlServer);
		con.Open();
		Console.WriteLine($"MySQL version : {con.ServerVersion}");
		// webserver
		// let's find an available port on the local machine, which is useful for unit tests
		var testUrl = server.AddAvailableLocalPrefix();
		Console.WriteLine(testUrl);
		// register our routes and handlers
		server.Handles(str => (str == "/print" || str.StartsWith("/print/")),async (context,cancellationToken) => {
				// print some debug info
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/plain";
			context.Request.GetClientCertificate(); // this has to be done!
			var bytes = Encoding.UTF8.GetBytes(ConcatAllTypes(context.Request));
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		server.Handles(str => (str == "/tables" || str.StartsWith("/tables/")),async (context,cancellationToken) => {
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/plain";
			var cmd = new MySqlCommand();
			cmd.Connection = con;
			cmd.CommandText = "SHOW TABLES";
			MySqlDataReader rdr = cmd.ExecuteReader();
			string data = "Tables:\n";
			while(rdr.Read()){
				data += $"{rdr.GetString(0)}\n";
			}
			byte[] bytes = Encoding.UTF8.GetBytes(data);
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		server.HandlesPath("/status", async (context, cancellationToken) => {
				// print current status
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/html";
			//TimeSpan upTime = DateTime.Now - serverStartTime;
			var bytes = Encoding.UTF8.GetBytes($"<html><body>"+
					$"MySQL version : {con.ServerVersion}<br>"+
					$"time at request : {DateTime.Now.ToString("HH:mm:ss")}<br>" + 
					//$"up-time : {upTime}s<br>"+
					$"up-time : {DateTime.Now - serverStartTime}s<br>"+
					"Current status : Running<br><br>"+
					// and some links
					"<a href=\"/stop\">Stop Server</a><br>"+
					"<a href=\"https://github.com/javidsho/LightHTTP\">LightHttp</a></body></html>");
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		server.HandlesPath("/stop", async (context, cancellationToken) => {
				// stops the server (you can also use Ctrl-C in the Console)
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/plain";
			var bytes = Encoding.UTF8.GetBytes($"Stopping!");
			keepAlive = false;
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		server.HandlesStaticFile("/main.css", "web-files/main.css");
		server.HandlesStaticFile("/", "web-files/index.html");
		server.HandlesStaticFile("/food", "web-files/food.html");
		server.HandlesStaticFile("/book", "web-files/book.html");
		server.HandlesStaticFile("/location", "web-files/location.html");
		server.HandlesStaticFile("/contact", "web-files/contact.html");

		// finally start serving
		server.Start();
		// https://medium.com/@rainer_8955/gracefully-shutdown-c-apps-2e9711215f6d
		Console.CancelKeyPress += (_, ea) =>
		{
			// Tell .NET to not terminate the process imidieatly
			ea.Cancel = true;

			Console.WriteLine("Received SIGINT (Ctrl+C)");
			keepAlive = false;
		};

		// and call server.Dispose() when done
		while(keepAlive){
			Thread.Sleep(100);
		}
		// kill Webserver
		server.Dispose();
		// kill MySql
		con.Close();
	}
*/

}
