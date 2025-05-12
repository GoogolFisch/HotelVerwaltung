using MySql.Data.MySqlClient;
using System.Collections.Generic;
using LightHTTP;
using System.Text;
using System.ComponentModel;
using System.Globalization;
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

		service.EnsureTableFormat("Kunden","Kunden_ID int(11) auto_increment,VorName varchar(20) NOT NULL ,NachName varchar(20) NOT NULL ,eMail varchar(40) NOT NULL ,ErstellungsDatum datetime NOT NULL ,GeborenAm date NOT NULL ,password char(64) NOT NULL ,PRIMARY KEY (Kunden_ID)");
		service.EnsureTableFormat("Raum","Raum_ID int(11) auto_increment,Kosten decimal(5,2) NOT NULL ,anzBetten int(2) NOT NULL ,RaumTyp varchar(2) NOT NULL ,ZimmerNum int(11) NOT NULL ,PRIMARY KEY (Raum_ID)");
		service.EnsureTableFormat("Buchungen","BuchungsID int(11) auto_increment,Kunden_ID int(11) NOT NULL ,BuchungsDatum datetime NOT NULL ,BuchungStart date NOT NULL ,BuchungEnde date NOT NULL ,PRIMARY KEY (BuchungsID)");
		service.EnsureTableFormat("ZimmerBuchung","Buchungs_ID int(11) ,Raum_ID int(11) ,PRIMARY KEY (Buchungs_ID,Raum_ID)");
		// register everything!
		// register funny logical stuff
		service.server.Handles(str => (str == "/print" || str.StartsWith("/print/")),async (context,cancellationToken) => {
				// print some debug info
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = "text/plain";
			context.Request.GetClientCertificate(); // this has to be done!
			string data = ConcatAllTypes(context.Request);
			data += "===========\n";
			data += ConcatAllTypes(context.Request.Cookies);
			data += "??\n";
			foreach(var cookie in context.Request.Cookies){
				data += $"{cookie}\n";
			}
			data += "===========\n";
			// get info from "form"-elements
			var hidParam = Servicer.GetHiddenParameters(context.Request);
			foreach(var key in hidParam.Keys)
				data += $"{key} -> {hidParam[key]}\n";
			var bytes = Encoding.UTF8.GetBytes(data);
			//var bytes = Encoding.UTF8.GetBytes(ConcatAllTypes(context.Request.QueryString));
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
				Console.WriteLine(data);
				data += ConcatAllTypes(schema);
				Console.WriteLine(data);
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
		service.server.HandlesPath("/try-register", async (context, cancellationToken) => {
			var hidParam = Servicer.GetHiddenParameters(context.Request);
			string data = "<!DOCTYPE html><html><body>";
			int retVal = service.TryRegisterUser(hidParam);
			if(retVal == 0){
				data += "registering successfull for:<br>";
				data += $"{hidParam["fname"]} ";
				data += $"{hidParam["lname"]}";
				data +=	"<script>setTimeout("+
				        "\"window.location.href='/'\",2500);</script>";
			} else if(retVal == 1){
				data += "some one already hash such an account";
				data +=	"<script>setTimeout("+
				        "\"window.location.href='/register'\",2500"+
					");</script>";
			}else if(retVal == 2){
				data += "Not correct format!";
				data +=	"<script>setTimeout("+
				        "\"window.location.href='/register'\",2500"+
					");</script>";
			}
			data += "</body></html>";
			context.Response.ContentType = "text/html";
			var bytes = Encoding.UTF8.GetBytes(data);
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		service.server.HandlesPath("/try-login", async (context, cancellationToken) => {
			var hidParam = Servicer.GetHiddenParameters(context.Request);
			string data = "<!DOCTYPE html><html><body>";
			bool retVal = service.TryLogin(hidParam);
			if(retVal){
				data += "success!";
				data +=	"<script>setTimeout("+
				        "\"window.location.href='/'\",2500"+
					");</script>";
			}
			else{
				data += "incorrect information!";
				data +=	"<script>setTimeout("+
				        "\"window.location.href='/login'\",2500"+
					");</script>";
			}

			var bytes = Encoding.UTF8.GetBytes(data);
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		service.server.HandlesPath("/try-book", async (context, cancellationToken) => {
				try{
			var hidParam = Servicer.GetHiddenParameters(context.Request);
			string data = "<!DOCTYPE html><html><body>";
			bool retVal = service.TryLogin(hidParam);
			if(retVal){
				data += "login successful!";
			}else{
				data += "wrong login!";
				goto END_TRY_BOOK;
			}
			DateTime starting = DateTime.Parse(hidParam["from"],CultureInfo.InvariantCulture);
			DateTime ending = DateTime.Parse(hidParam["till"],CultureInfo.InvariantCulture);
			Console.WriteLine($"{starting} - {ending}");
			if(starting > ending){
				data += "\nout of order!";
				goto END_TRY_BOOK;
			}
			if(ending < DateTime.Now){ // add a min time!
				data += "\ncannot book in the past!";
				goto END_TRY_BOOK;
			}
			// SELECT * FROM Raum Where Raum_ID NOT IN (SELECT Raum_ID FROM ZimmerBuchung zb JOIN Buchungen b ON zb.Buchungs_ID = b.BuchungsID WHERE b.BuchungStart > "2025-02-02" AND b.BuchungEnde < "2025-03-03")
			string sql = "";
			bool canDoIt = true;
			MySqlCommand cmd = new MySqlCommand(sql,service.con);
			MySqlDataReader pref;
			foreach(RoomInfos rmInf in service.roomTypes){
				int wantingCnt = Convert.ToInt32(
						hidParam[$"snd-{rmInf.typeName}"]
						);
				cmd.CommandText = $"SELECT COUNT(*) FROM Raum Where Raum_ID NOT IN (SELECT Raum_ID FROM ZimmerBuchung zb JOIN Buchungen b ON zb.Buchungs_ID = b.BuchungsID WHERE b.BuchungStart > \"{starting.ToString("yyyy-MM-dd")}\" AND b.BuchungEnde < \"{ending.ToString("yyyy-MM-dd")}\") AND RaumTyp =\"{rmInf.typeName}\" ";
				pref = cmd.ExecuteReader();
				pref.Read();
				int count = pref.GetInt32(0);
				//Console.WriteLine($"{count} / {wantingCnt}");
				canDoIt &= (count >= wantingCnt);
				pref.Dispose();
				pref.Close();
			}
			if(!canDoIt){
				data += "Sie haben mehr Raeume gebucht, als moeglich sind!";
				goto DISPOSE_CMD_BOOK;
			}
			cmd.CommandText = $"INSERT INTO Buchungen(Kunden_ID,BuchungsDatum,BuchungStart,BuchungEnde) VALUES((SELECT Kunden_ID FROM Kunden WHERE Kunden.eMail = \"{hidParam["mail"]}\"),\"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\"," + 
			$"\"{starting.ToString("yyyy-MM-dd")}\",\"{ending.ToString("yyyy-MM-dd")}\")";
			cmd.ExecuteNonQuery();
			cmd.CommandText = $"SELECT BuchungsID FROM Buchungen WHERE Kunden_ID = (SELECT Kunden_ID FROM Kunden WHERE Kunden.eMail = \"{hidParam["mail"]}\")";
			pref = cmd.ExecuteReader();
			pref.Read();
			int buchungsId = pref.GetInt32(0);
			pref.Dispose();
			pref.Close();
			foreach(RoomInfos rmInf in service.roomTypes){
				cmd.CommandText = $"SELECT Raum_ID FROM Raum Where Raum_ID NOT IN (SELECT Raum_ID FROM ZimmerBuchung zb JOIN Buchungen b ON zb.Buchungs_ID = b.BuchungsID WHERE b.BuchungStart > \"{starting.ToString("yyyy-MM-dd")}\" AND b.BuchungEnde < \"{ending.ToString("yyyy-MM-dd")}\") AND RaumTyp =\"{rmInf.typeName}\" ";
				pref = cmd.ExecuteReader();
				List<int> roomIds = new List<int>();
				while(pref.Read())
					roomIds.Add(pref.GetInt32(0));
				pref.Dispose();
				pref.Close();
				//
				int counting = Convert.ToInt32(hidParam[$"snd-{rmInf.typeName}"]);
				for(int i = 0;i < counting;i++){
					cmd.CommandText = $"INSERT INTO ZimmerBuchung(Buchungs_ID,Raum_ID) Values ({buchungsId},{roomIds[i]})";
					cmd.ExecuteNonQuery();
				}
			}

DISPOSE_CMD_BOOK:
			cmd.Dispose();
END_TRY_BOOK:
			data += "</body></html>";
			var bytes = Encoding.UTF8.GetBytes(data);
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
			}catch(Exception e){Console.WriteLine(e.ToString());}
		});
		// from service.server.HandlesStaticFile("/book", "web-files/book.html");
		service.server.HandlesPath("/book", async (context, cancellationToken) => {
			string data = "<!DOCTYPE html><html>" +
			"<head>" +
			"	<link rel=\"stylesheet\" href=\"main.css\"/>" +
			"	<script src=\"/scripts/booking.js\"></script>" +
			"</head><body>" +
			"	<div class=\"navbar\">" +
			"		<a href=\"/\">Home</a>" +
			"		<a href=\"/book\">Buchen</a>" +
			"		<a href=\"/food\">Restaurante</a>" +
			"		<a href=\"/location\">Lageplan</a>" +
			"		<a href=\"/contact\">Ansprechpartner</a>" +
			"		<a class=\"right\" href=\"/login\">Login</a>" +
			"	</div>" +
			"	<main class=\"main\">" +
			"		<h1>Zimmer buchen</h1>";
			//"		%%"; // insert suff here!
			// insert auto creation of stuff here!
			data += "<ul>";
			foreach(RoomInfos roomTyp in service.roomTypes){
				data += "<li class=\"rooms\">" +
					$"{roomTyp.ToHtml()}" +
					$"<script>roomTypes.set(\"{roomTyp.typeName}\",{roomTyp.cost});</script>" +
					"</li>\n";
			}
			data += "</ul>";
			// adding booking stuff
			data +=
			"<form method=\"post\" role=\"form\" action=\"/try-book\">" +
				"<label for=\"from\">Datum von:</label>" +
				"<input type=\"date\" id=\"from\" name=\"from\"></input><br>" +
				"<label for=\"till\">Datum bis:</label>" +
				"<input type=\"date\" id=\"till\" name=\"till\"></input><br>" +
				"<label for=\"mail\">E-Mail:</label>" +
				"<input type=\"email\" id=\"mail\" name=\"mail\"></input><br>" +
				"<label for=\"pwd\">Passwort:</label>" +
				"<input type=\"password\" id=\"pwd\" name=\"pwd\"></input><br>" +
				"<div class=\"flex-down\">" +
				"<div id=\"costing\">Kostet: $0</div>" +
				"<button>Buchen!</button>";
			foreach(RoomInfos roomTyp in service.roomTypes){
				data += "<input type=\"hidden\""+
					$"name=\"snd-{roomTyp.typeName}\"" +
					$"id=\"snd-{roomTyp.typeName}\">";
			}
			data += "</div>" +
			"</form>";
			data += 
			"	</main></body></html>";
			var bytes = Encoding.UTF8.GetBytes(data);
			await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		});
		service.server.Handles(
				path => (path.StartsWith("/images/") ||
					 path.StartsWith("/scripts/")) &&
					(!path.Contains("..")),
		async (context, cancellationToken) => {
			//Console.WriteLine(context.Request.RawUrl);
			using var fileStream = new FileStream(
				$".{context.Request.RawUrl}", FileMode.Open
			);
			await fileStream.CopyToAsync(
				context.Response.OutputStream, 81920,
				cancellationToken
			).ConfigureAwait(false);
		});
		// register local-files
		service.server.HandlesStaticFile("/main.css", "web-files/main.css");
		service.server.HandlesStaticFile("/", "web-files/index.html");
		service.server.HandlesStaticFile("/food", "web-files/food.html");
		service.server.HandlesStaticFile("/location", "web-files/location.html");
		service.server.HandlesStaticFile("/contact", "web-files/contact.html");
		service.server.HandlesStaticFile("/login", "web-files/login.html"); // move to handler!
		service.server.HandlesStaticFile("/register", "web-files/register.html"); // move to handler!

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
}
