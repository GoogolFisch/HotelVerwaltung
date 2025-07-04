﻿using MySql.Data.MySqlClient;
using System.Collections.Generic;
using LightHTTP;
using System.Text;
using System.ComponentModel;
using System.Net;
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
	public const int CancelationTime = 3;
	public const string docStart = "<!DOCTYPE html> <meta charset=\"UTF-8\"> <html><head>" +
		"<link rel=\"stylesheet\" href=\"/main.css\">" +
		"</head><body>" +
		"<div class=\"navbar\">" +
		"	<a href=\"/\">Home</a>" +
		"	<a href=\"/book\">Buchen</a>" +
		"	<a href=\"/food\">Restaurante</a>" +
		"	<a href=\"/location\">Lageplan</a>" +
		"	<a href=\"/contact\">Ansprechpartner</a>" +
		"	<a class=\"right\" href=\"/login\">Login</a>" +
		"</div><main>";
	public const string docEnd = "</main></body></html>";
	
	// this is IDE replacement
	public static string ConcatAllTypes(object obj){
		// name
		string concar = $"{obj}\n---------------\n";
		// class variables
		foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
		{
			string name = descriptor.Name;
			object? value = descriptor.GetValue(obj);
			// append the values
			if(value is null)
				// if null do this
				concar += $"{name}\t=\t<null>\n";
			else
				// else use the ToString()
				concar += $"{name}\t=\t{value}\n";
		}
		concar += "---------------\n";
		//class methods
		Type type = obj.GetType();

		foreach (var method in type.GetMethods())
		{
			var parameters = method.GetParameters();
			// this can look easy
			var parameterDescriptions = string.Join
			(", ", method.GetParameters()
			.Select(x => x.ParameterType + " " + x.Name)
			.ToArray());
			// append values of the functions
			concar += $"{method.ReturnType}\t{method.Name}\t{parameterDescriptions}\n";
		}
		return concar;
	}
	public static void Main(string []arguments){
		serverStartTime = DateTime.Now;
		service = new Servicer();

		Console.WriteLine(service.webServerUrl);

		// ensure table formats
		service.EnsureTableFormat("Kunden","CREATE TABLE `Kunden` ( `Kunden_ID` int(11) NOT NULL AUTO_INCREMENT, `VorName` varchar(20) NOT NULL, `NachName` varchar(20) NOT NULL, `eMail` varchar(40) NOT NULL, `ErstellungsDatum` datetime NOT NULL, `GeborenAm` date NOT NULL, `password` char(64) NOT NULL, PRIMARY KEY (`Kunden_ID`), UNIQUE KEY `eMail` (`eMail`))");
		service.EnsureTableFormat("Raum","CREATE TABLE `Raum` ( `Raum_ID` int(11) NOT NULL AUTO_INCREMENT, `RaumTyp` varchar(2) NOT NULL, `ZimmerNum` int(11) NOT NULL, PRIMARY KEY (`Raum_ID`), KEY `RaumTypenRefrence` (`RaumTyp`), CONSTRAINT `RaumTypenRefrence` FOREIGN KEY (`RaumTyp`) REFERENCES `RaumTypen` (`RaumTyp`))");
		service.EnsureTableFormat("Buchungen","CREATE TABLE `Buchungen` ( `Buchungs_ID` int(11) NOT NULL AUTO_INCREMENT, `Kunden_ID` int(11) NOT NULL, `BuchungsDatum` datetime NOT NULL, `BuchungStart` date NOT NULL, `BuchungEnde` date NOT NULL, PRIMARY KEY (`Buchungs_ID`), CONSTRAINT `ReferenzAufKunden` FOREIGN KEY (`Kunden_ID`) REFERENCES `Kunden` (`Kunden_ID`) ON DELETE NO ACTION ON UPDATE NO ACTION)");
		service.EnsureTableFormat("ZimmerBuchung","CREATE TABLE `ZimmerBuchung` ( `Buchungs_ID` int(11) NOT NULL, `Raum_ID` int(11) NOT NULL, PRIMARY KEY (`Buchungs_ID`,`Raum_ID`), KEY `ReferenzAufRaum` (`Raum_ID`), CONSTRAINT `BuchungsDependence` FOREIGN KEY (`Buchungs_ID`) REFERENCES `Buchungen` (`Buchungs_ID`) ON DELETE CASCADE ON UPDATE NO ACTION, CONSTRAINT `ReferenzAufRaum` FOREIGN KEY (`Raum_ID`) REFERENCES `Raum` (`Raum_ID`) ON DELETE NO ACTION ON UPDATE NO ACTION)");
		service.EnsureTableFormat("Bewertung","CREATE TABLE `Bewertung` ( `Kunden_ID` int(11) NOT NULL, `Sterne` int(1) NOT NULL, `Nachricht` text NOT NULL, PRIMARY KEY (`Kunden_ID`), CONSTRAINT `DeleteOnKundenDelete` FOREIGN KEY (`Kunden_ID`) REFERENCES `Kunden` (`Kunden_ID`) ON DELETE CASCADE ON UPDATE NO ACTION)");
		service.EnsureTableFormat("RaumTypen","CREATE TABLE `RaumTypen` ( `RaumTyp` varchar(2) NOT NULL, `Kosten` decimal(6,2) NOT NULL, `AnzBetten` int(2) NOT NULL, PRIMARY KEY (`RaumTyp`))");
		// adding extra XXX
		// register everything!
		// register funny logical stuff
		service.server.Handles(str => (str == "/print" || str.StartsWith("/print/")),HandelHttpPrint);
		service.server.HandlesPath("/status", HandelHttpStatus);
		service.server.HandlesPath("/try-register", HandelHttpRegister);
		service.server.HandlesPath("/register", HandelHttpRegister);
		service.server.HandlesPath("/try-login", HandelHttpLogin);
		service.server.HandlesPath("/login", HandelHttpLogin);
		service.server.Handles( path => path.StartsWith("/account/"), HandelHttpAccount);
		service.server.HandlesPath("/try-book", HandelHttpBook);
		service.server.HandlesPath("/try-food", async (context, cancellationToken) => { HandelHttpStatus(context, cancellationToken); });
		// from service.server.HandlesStaticFile("/book", "web-files/book.html");
		service.server.HandlesPath("/book", HandelHttpBook);
		// load other data
		service.server.Handles(
				path => (path.StartsWith("/images/") ||
					 path.StartsWith("/scripts/")) &&
					(!path.Contains("..")),
		async (context, cancellationToken) => {
			// stolen from LightHTTP
			using var fileStream = new FileStream(
				$".{context.Request.RawUrl}", FileMode.Open
			);
			await fileStream.CopyToAsync(
				context.Response.OutputStream, 81920,
				cancellationToken
			).ConfigureAwait(false);
		});
		//
		service.server.HandlesPath("/", HandelHttpIndex);
		// register local-files
		service.server.HandlesStaticFile("/main.css", "web-files/main.css");
		//service.server.HandlesStaticFile("/", "web-files/index.html");
		service.server.HandlesStaticFile("/food", "web-files/food.html");
		service.server.HandlesStaticFile("/location", "web-files/location.html");
		service.server.HandlesStaticFile("/contact", "web-files/contact.html");
		//service.server.HandlesStaticFile("/login", "web-files/login.html"); // move to handler!
		//service.server.HandlesStaticFile("/register", "web-files/register.html"); // move to handler!
		service.server.HandlesStaticFile("/favicon.ico", "web-files/favicon-icon-192x192.png");

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
		// this should overflow!
		short sleeperIter = 0;
		//
		while(keepAlive){
			Thread.Sleep(100);
			sleeperIter++;
			if(sleeperIter == 0){
				// update wich rooms exits, by now and then
				service.ReadAllRoomTypes();
			}
		}
		// stopping
		service.Stop();
	}
	private static async Task HandelHttpPrint(HttpListenerContext context,CancellationToken cancellationToken){
		// setup stuff
		context.Response.ContentEncoding = Encoding.UTF8;
		context.Response.ContentType = "text/plain";
		context.Request.GetClientCertificate(); // this has to be done!
		// get Request data
		string document = ConcatAllTypes(context.Request);
		document += "===========\n";
		// get Response stuff?
		document += ConcatAllTypes(context.Response);
		document += "===========\n";
		// get Cookies, we don't use them
		document += ConcatAllTypes(context.Request.Cookies);
		document += "??\n";
		// list cookies
		foreach(var cookie in context.Request.Cookies){
			document += $"{cookie}\n";
		}
		document += "===========\n";
		// get info from "form"-elements
		var hidParam = Servicer.GetHiddenParameters(context.Request);
		foreach(var key in hidParam.Keys)
			document += $"{key} -> {hidParam[key]}\n";
		var bytes = Encoding.UTF8.GetBytes(document);
		//var bytes = Encoding.UTF8.GetBytes(ConcatAllTypes(context.Request.QueryString));
		await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		return;
	}
	private static async Task HandelHttpStatus(HttpListenerContext context,CancellationToken cancellationToken){
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
				"<a href=\"https://github.com/javidsho/LightHTTP\">LightHttp</a>" + Program.docEnd);
		await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		return ;
	}
	private static async Task HandelHttpRegister(HttpListenerContext context,CancellationToken cancellationToken){
		context.Response.ContentEncoding = Encoding.UTF8;
		context.Response.ContentType = "text/html";
		string document = Program.docStart;
		// if pushed the form, then
		if(context.Request.HttpMethod == "POST"){
			var hidParam = Servicer.GetHiddenParameters(context.Request);
			int retVal = service.TryRegisterUser(hidParam);
			if(retVal == 0){
				// show if successful
				document += "Erfolgreich erstellung von:<br>";
				document += $"{hidParam["fname"]} ";
				document += $"{hidParam["lname"]}";
				document +=	"<script>setTimeout("+
				        "\"window.location.href='/'\",1000);</script>";
			} else if(retVal == 1){
				// show 
				document += "Jemandem hat schon diese E-Mail registiert.";
				document +=	"<script>setTimeout("+
				        "\"window.location.href='/register'\",1000"+
					");</script>";
			}else if(retVal == 2){
				document += "Falsches Format!";
				document +=	"<script>setTimeout("+
				        "\"window.location.href='/register'\",1000"+
					");</script>";
			}
		}else{
			// get request, send the form
			document += 
"<form action=\"/register\" method=\"post\" role=\"form\">" +
"	<label for=\"fname\">Erstname:</label>" +
"	<input type=\"text\" id=\"fname\" name=\"fname\" required></input><br>" +
"	<label for=\"lname\">Nachname:</label>" +
"	<input type=\"text\" id=\"lname\" name=\"lname\" required></input><br>" +
"	<label for=\"e-mail\">E-Mail:</label>" +
"	<input type=\"email\" id=\"e-mail\" name=\"mail\" required></input><br>" +
"	<label for=\"birth\">Geburtstag:</label>" +
"	<input type=\"date\" id=\"birth\" name=\"birth\" required></input><br>" +
"	<label for=\"pwd\">Passwort:</label>" +
"	<input type=\"password\" id=\"pwd\" name=\"pwd\" required></input><br>" +
"	<input type=\"submit\" value=\"submit\">" +
"</form>" +
"<a href=\"login\">Login</a>";
		}
		// sende die HTML seite
		document += Program.docEnd;
		context.Response.ContentType = "text/html";
		byte[] bytes = Encoding.UTF8.GetBytes(document);
		await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
	}
	private static async Task HandelHttpLogin(HttpListenerContext context,CancellationToken cancellationToken){
		context.Response.ContentEncoding = Encoding.UTF8;
		context.Response.ContentType = "text/html";
		string document = Program.docStart;
		//try{
		if(context.Request.HttpMethod == "POST"){
			// test the given password
			var hidParam = Servicer.GetHiddenParameters(context.Request);
			bool retVal = service.TryLogin(hidParam);
			if(retVal){
				document += "Erfolgreich!";
				string tokString = service.GetTokenFor(hidParam["mail"]);
				// also give them the token
				context.Response.RedirectLocation = $"/account/{tokString}";
				document += $"<script>setTimeout(\"window.location.href='/account/{tokString}'\",100);</script>";
			}
			else{
				// wrong do it agian
				document += "Falsche Login details.";
				document +=	"<script>setTimeout("+
				        "\"window.location.href='/login'\",1000"+
					");</script>";
			}
		}else{
			// on get request, send the form
			document += 
"<form action=\"/login\" method=\"post\" role=\"form\">" +
"	<label for=\"e-mail\">E-Mail:</label>" +
"	<input type=\"email\" id=\"e-mail\" name=\"mail\" required></input><br>" +
"	<label for=\"pwd\">Passwort:</label>" +
"	<input type=\"password\" id=\"pwd\" name=\"pwd\" required></input><br>" +
"	<input type=\"submit\" value=\"submit\">" +
"</form>" +
"<a href=\"register\">registreire ein Account</a>";

		}
		// send the HTML
		document += docEnd;
		var bytes = Encoding.UTF8.GetBytes(document);
		await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		//}catch(Exception e){Console.WriteLine(e.ToString());}
	}
	private static void HandelPostAccount(ref string document,HttpListenerContext context,int accId){
		var hidParam = Servicer.GetHiddenParameters(context.Request);
		// try login to validate
		bool retVal = service.TryLogin(hidParam);
		if(!retVal){
			document += "Not correct password!";
			//goto ACCOUNT_POST_END;
			return;
		}
		string fName = hidParam["fname"];
		string lName = hidParam["lname"];
		// stoping the db of beeing angry
		if(fName.Length > 19 || lName.Length > 19){
			document += "Ihr Name ist viel zu lang!";
			return;
		}
		string eMail = hidParam["mail"];
		string birth = hidParam["birth"];
		// check if both are correct / the same
		string newpwd = Servicer.EncodePassword(hidParam["npwd"]);
		string n2pwd = Servicer.EncodePassword(hidParam["n2pwd"]);
		if(newpwd != n2pwd){
			document += "Password don't match!";
			return;
		}
		// default (old) password
		if(hidParam["npwd"] == ""){
			newpwd = Servicer.EncodePassword(hidParam["pwd"]);
		}
		// update
		MySqlCommand cmd = new MySqlCommand($"UPDATE Kunden SET VorName=\"{fName}\",NachName=\"{lName}\",eMail=\"{eMail}\",GeborenAm=\"{birth}\",password=\"{newpwd}\" WHERE Kunden_ID = {accId}",service.con);
		cmd.ExecuteNonQuery();

		// don't get locked out!
		context.Response.RedirectLocation = $"/account/{service.GetTokenFor(accId)}";
		// indicate it worked
		document += "Changed stuff!";
		cmd.Dispose();
	}
	private static void HandelAccountCancelBooking(HttpListenerContext context,int accId,ref string document,int bookingId){
		//Console.WriteLine($"Removing: booking={bookingId} and account={accId}");
		MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Buchungen WHERE Kunden_ID = {accId} AND Buchungs_ID = {bookingId}",service.con);
		MySqlDataReader pref = cmd.ExecuteReader();
		// can delete
		bool shouldDelete = pref.HasRows;
		pref.Dispose();
		pref.Close();
		if(shouldDelete){
			// delete the stuff
			cmd.CommandText = $"DELETE FROM Buchungen WHERE Kunden_ID = {accId} AND Buchungs_ID = {bookingId};";
			cmd.ExecuteNonQuery();
			cmd.CommandText = $"DELETE FROM ZimmerBuchung WHERE Buchungs_ID = {bookingId};";
			cmd.ExecuteNonQuery();
		}
		cmd.Dispose();
	}
	private static void HandelAccountDeletion(HttpListenerContext context, int accId,ref string document){
		// test if the user really wants to do it
		Dictionary<string,string> hidParam = Servicer.GetHiddenParameters(context.Request);
		bool retVal = service.TryLogin(hidParam);
		if(!retVal){
			document += "Falsche EMail oder Passwort!<br>Nichts wird gelöscht!";
			// escape the deletion
			return;
		}
		// delete
		MySqlCommand cmd = new MySqlCommand($"DELETE FROM Kunden WHERE Kunden_Id = {accId}",service.con);
		cmd.ExecuteNonQuery();
		// also remove Bookings that are in the future
		cmd.CommandText = $"DELETE FROM Buchungen WHERE Kunden_Id = {accId} AND BuchungStart > {DateTime.Now.AddDays(CancelationTime).ToString(Servicer.yyyymmdd)}";
		cmd.ExecuteNonQuery();
		document += "Ihr account wurde gelöscht!";
		document +=	"<script>setTimeout("+
			"\"window.location.href='/'\",1000);</script>";
		cmd.Dispose();
	}
	private static async Task HandelHttpAccount(HttpListenerContext context,CancellationToken cancellationToken){
		try{
		// TODO split into seperate functions!
		//try{
		// check if is allowed
		string[] splittings = context.Request.RawUrl.Split('/');
		if(splittings.Length < 2){
			context.Response.StatusCode = 403;
			return;
		}
		string tok = splittings[2];
		string document = Program.docStart;
		int accId;
		// bail out if wrong token
		bool correctToken = service.CheckToken(tok,out accId);
		context.Response.ContentEncoding = Encoding.UTF8;
		context.Response.ContentType = "text/html";
		if(!correctToken){
			context.Response.StatusCode = 403;
			return;
		}
		// cancel
		if(splittings.Length > 3){
			if(splittings[3].StartsWith("storno-")){
				// get booking id
				int.TryParse(splittings[3].Substring(7),out int bookingNum);
				// cancel it if posible
				HandelAccountCancelBooking(context,accId,ref document,bookingNum);
			}else if(splittings[3].StartsWith("delete-account")){
				// try delete account
				HandelAccountDeletion(context,accId,ref document);
				// then bail out?
				document += Program.docEnd;
				var pre_bytes = Encoding.UTF8.GetBytes(document);
				await context.Response.OutputStream.WriteAsync(pre_bytes, 0, pre_bytes.Length);
				return;
			}
		}
		MySqlCommand cmd;
		// changing customer data
		if(context.Request.HttpMethod == "POST"){
			HandelPostAccount(ref document,context,accId);
		}
		//Console.WriteLine(context.Request.RawUrl);
		cmd = new MySqlCommand($"SELECT Kunden_ID, VorName, NachName, eMail, GeborenAm, ErstellungsDatum From Kunden Where Kunden_ID = {accId}",service.con);
		MySqlDataReader pref = cmd.ExecuteReader();
		pref.Read();
		// displaying hello
		document += "<div class=\"flexing-left\">" +
		$"<div id=\"account\">Hello {Servicer.DecodeEscaped(pref.GetString(1))} {Servicer.DecodeEscaped(pref.GetString(2))}<br>" +
		$"E-Mail: {Servicer.DecodeEscaped(pref.GetString(3))}<br>" +
		$"Geboren Am: {pref.GetDateTime(4).ToString(Servicer.ddmmyyyy)}<br>" +
		$"Account_ID:{pref.GetInt32(0)}<br>" +
		$"Erstellt am:{pref.GetDateTime(5).ToString($"{Servicer.ddmmyyyy} {Servicer.hhmmss}")}<br>" +
		$"<button onclick=\"accountStartEdit()\">Editiere</button>" +
		$"<button onclick=\"accountDeletionStart()\" class=\"left-space\">Löschen</button></div>" +
		"<script src=\"/scripts/account-edit.js\"></script>";
		pref.Close();
		pref.Dispose();
		// you can remove your rating XXX
		document += "<div class=\"Bewertung\">";
		cmd.CommandText = $"SELECT Sterne,Nachricht FROM Bewertung WHERE Kunden_ID = {accId}";
		pref = cmd.ExecuteReader();
		if(pref.Read()){
			// show your rating
			document += "Sie haben eine Bewertung hinterlegt.<br>";
			document += $"Bewertung: {pref.GetInt32(0)} Sterne <br> {Servicer.DecodeEscaped(pref.GetString(1))}";
		}
		document += "<div id=\"deletionForm\"></div></div></div>";
		pref.Close();
		pref.Dispose();
		// lising each booking
		List<BookingInfo> bkInfos = service.GetBookingFromKundenID(accId);

		document += "<div style=\"width:fit-content\">";
		decimal kosten;
		string addLaterData;
		// insert each room into each booking order
		foreach(BookingInfo bkinf in bkInfos){
			kosten = 0m;
			cmd.CommandText = $"SELECT rt.Kosten,rt.anzBetten,r.RaumTyp,r.ZimmerNum FROM Raum r JOIN ZimmerBuchung zb ON r.Raum_ID = zb.Raum_ID JOIN RaumTypen rt ON rt.RaumTyp = r.RaumTyp WHERE zb.Buchungs_ID = {bkinf.bookingId}";
			pref = cmd.ExecuteReader();
			int daySpan = (bkinf.bookingEnd - bkinf.bookingStart).Days + 1;
			addLaterData = "<ul>";
			// insert the rooms
			while(pref.Read()){
				// get costs from each room
				kosten += (decimal)pref.GetFloat(0) * daySpan;
				addLaterData += $"<li>Raum-{pref.GetString(2)}: " +
					$"{pref.GetInt32(3)} " +
					$"mit {pref.GetInt32(1)} Betten -> " +
					$"${pref.GetFloat(0)}</li>";
			}
			addLaterData += "</ul>";
			document +=
			"<div class=\"boxing\">" + 
			$"Gebucht am: {bkinf.bookingDate.ToString($"{Servicer.ddmmyyyy} {Servicer.hhmmss}")}<br>" +
			$"{bkinf.bookingStart.ToString(Servicer.ddmmyyyy)} - {bkinf.bookingEnd.ToString(Servicer.ddmmyyyy)}" +
			$" | ${kosten}" +
			$"{addLaterData}";
			// last cancel time test
			if(bkinf.bookingStart > DateTime.Now)
				document += $"<button onclick=\"cancelBooking({bkinf.bookingId});\">Stornieren</button>";
			else
				document += $"Stornieren nicht möglich.";
			document += "</div>";

			pref.Close();
			pref.Dispose();
			//document += $"";
		}
		// send the HTML
		cmd.Dispose();
		document += Program.docEnd;
		var bytes = Encoding.UTF8.GetBytes(document);
		await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		}catch(Exception e){Console.WriteLine(e.ToString());}
	}

	private static void HandelPostBookQuerying(ref string document,Dictionary<string,string> hidParam,DateTime starting,DateTime ending){
		// SELECT * FROM Raum Where Raum_ID NOT IN (SELECT Raum_ID FROM ZimmerBuchung zb JOIN Buchungen b ON zb.Buchungs_ID = b.Buchungs_ID WHERE b.BuchungStart > "2025-02-02" AND b.BuchungEnde < "2025-03-03")
		bool canDoIt = true;
		MySqlCommand cmd = new MySqlCommand("",service.con);
		MySqlDataReader pref;
		// test if enougth rooms are avalible
		foreach(RoomInfos rmInf in service.roomTypes){
			if(hidParam[$"snd-{rmInf.typeName}"] == "")
				hidParam[$"snd-{rmInf.typeName}"] = "0";
			int wantingCnt = Convert.ToInt32(
					hidParam[$"snd-{rmInf.typeName}"]
					);
			// XXX, currently from Raum
			cmd.CommandText = $"SELECT COUNT(*) FROM Raum Where Raum_ID NOT IN (SELECT Raum_ID FROM ZimmerBuchung zb JOIN Buchungen b ON zb.Buchungs_ID = b.Buchungs_ID WHERE b.BuchungStart > \"{starting.ToString(Servicer.yyyymmdd)}\" AND b.BuchungEnde < \"{ending.ToString(Servicer.yyyymmdd)}\") AND RaumTyp =\"{rmInf.typeName}\" ";
			pref = cmd.ExecuteReader();
			pref.Read();
			int count = pref.GetInt32(0);
			// anding everything
			canDoIt &= (count >= wantingCnt);
			pref.Dispose();
			pref.Close();
		}
		if(!canDoIt){
			// bail out
			document += "Sie haben mehr Räume gebucht, als möglich sind!";
			goto DISPOSE_CMD_BOOK;
		}
		// add an booking base
		string sanMail = hidParam["mail"];
		cmd.CommandText = $"INSERT INTO Buchungen(Kunden_ID,BuchungsDatum,BuchungStart,BuchungEnde) VALUES((SELECT Kunden_ID FROM Kunden WHERE Kunden.eMail = \"{sanMail}\"),\"{DateTime.Now.ToString($"{Servicer.yyyymmdd} {Servicer.hhmmss}")}\"," + 
		$"\"{starting.ToString(Servicer.yyyymmdd)}\",\"{ending.ToString(Servicer.yyyymmdd)}\")";
		cmd.ExecuteNonQuery();
		cmd.CommandText = $"SELECT MAX(Buchungs_ID) FROM Buchungen WHERE Kunden_ID = (SELECT Kunden_ID FROM Kunden WHERE Kunden.eMail = \"{sanMail}\")";
		pref = cmd.ExecuteReader();
		pref.Read();
		int buchungsId = pref.GetInt32(0);
		pref.Dispose();
		pref.Close();
		// add each room to booking
		foreach(RoomInfos rmInf in service.roomTypes){
			cmd.CommandText = $"SELECT Raum_ID FROM Raum Where Raum_ID NOT IN (SELECT Raum_ID FROM ZimmerBuchung zb JOIN Buchungen b ON zb.Buchungs_ID = b.Buchungs_ID WHERE b.BuchungStart > \"{starting.ToString(Servicer.yyyymmdd)}\" AND b.BuchungEnde < \"{ending.ToString(Servicer.yyyymmdd)}\") AND RaumTyp =\"{rmInf.typeName}\" ";
			pref = cmd.ExecuteReader();
			List<int> roomIds = new List<int>();
			while(pref.Read())
				roomIds.Add(pref.GetInt32(0));
			pref.Dispose();
			pref.Close();
			// book each room into an booking
			int counting = Convert.ToInt32(hidParam[$"snd-{rmInf.typeName}"]);
			for(int i = 0;i < counting;i++){
				cmd.CommandText = $"INSERT INTO ZimmerBuchung(Buchungs_ID,Raum_ID) Values ({buchungsId},{roomIds[i]})";
				cmd.ExecuteNonQuery();
			}
		}

DISPOSE_CMD_BOOK:
		cmd.Dispose();
	}
	private static async Task HandelPostBook(HttpListenerContext context,CancellationToken cancellationToken){
		try{
		// test the account
		Dictionary<string,string> hidParam = Servicer.GetHiddenParameters(context.Request);
		string document = Program.docStart;
		bool retVal = service.TryLogin(hidParam);
		if(!retVal){
			document += "Falsches EMail oder Passwort!";
			goto END_TRY_BOOK;
		}
		// date checking
		DateTime starting = DateTime.Parse(hidParam["from"],CultureInfo.InvariantCulture);
		DateTime ending = DateTime.Parse(hidParam["till"],CultureInfo.InvariantCulture);
		// no out of order booking
		if(starting > ending){
			document += "\nNegativer Buchungszeitraum";
			goto END_TRY_BOOK;
		}
		if(ending < DateTime.Now){ // add a min time!
			document += "\nIn vergangenen Tagen kann man nicht buchen";
			goto END_TRY_BOOK;
		}
		if(ending < DateTime.Now.AddDays(CancelationTime)){ // add a min time!
			document += "\nNach der Stornierfrist kann nicht mehr gebucht werden.";
			goto END_TRY_BOOK;
		}
		// taken the happy path
		document += "Ihre Buchung war erfolgreich!";
		// do the booking
		HandelPostBookQuerying(ref document,hidParam,starting,ending);
END_TRY_BOOK:
		// finishing touches
		document += Program.docEnd;
		var bytes = Encoding.UTF8.GetBytes(document);
		await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		}catch(Exception e){Console.WriteLine(e.ToString());}
	}
	private static async Task HandelGetBook(HttpListenerContext context,CancellationToken cancellationToken){
		// send the booking HTML with the form
		string document = Program.docStart +
		"<script src=\"/scripts/booking.js\"></script>" +
		"	<h1>Zimmer buchen</h1>";
		//"		%%"; // insert suff here!
		// insert auto creation of stuff here!
		document += "<ul>";
		foreach(RoomInfos roomTyp in service.roomTypes){
			document += roomTyp.ToHtml();
		}
		document += "</ul>";
		// adding booking stuff
		DateTime tomorrow = DateTime.Now.AddDays(CancelationTime); // auto limit the input
		document +=
		"<form method=\"post\" role=\"form\" action=\"/book\">" +
			"<label for=\"from\">Datum von:</label>" +
			$"<input type=\"date\" id=\"from\" name=\"from\" onchange=\"total_update()\" min=\"{tomorrow.ToString(Servicer.yyyymmdd)}\" value=\"{tomorrow.ToString(Servicer.yyyymmdd)}\" required></input><br>";
		// limit the till part
		document +=
			"<label for=\"till\">Datum bis:</label>" +
			$"<input type=\"date\" id=\"till\" name=\"till\" onchange=\"total_update()\" min=\"{tomorrow.AddDays(1).ToString(Servicer.yyyymmdd)}\" required></input><br>" +
			"<label for=\"mail\">E-Mail:</label>" +
			"<input type=\"email\" id=\"mail\" name=\"mail\" required></input><br>" +
			"<label for=\"pwd\">Passwort:</label>" +
			"<input type=\"password\" id=\"pwd\" name=\"pwd\" required></input><br>" +
			"<div class=\"flex-down\">" +
			"<div id=\"costing\">Kostet: $0</div>" +
			"<button>Buchen!</button>";
		// pre safed room loop
		foreach(RoomInfos roomTyp in service.roomTypes){
			document += "<input type=\"hidden\""+
				$"name=\"snd-{roomTyp.typeName}\"" +
				$"id=\"snd-{roomTyp.typeName}\">";
		}
		document += "</div>" +
		"</form>";
		// cancelation notice
		document += $"Das Buchen ist nur {CancelationTime} Tage vor dem eigentlichem Tag möglich<br>" +
			$"Das Stornieren ist auch nur bis zu {CancelationTime} Tage vorher möglich, <br>" +
			"Ihr Zimmer ist um 16:00 am Anreisetag verfügbar,<br>und um 10:00 am Abreisetag müssen Sie den Raum verlassen haben." +
			"Stornieren können Sie auf Ihrer <a href=\"/login\">Account</a> Seite.";
		document += Program.docEnd;
		var bytes = Encoding.UTF8.GetBytes(document);
		await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
	}
	private static async Task HandelHttpBook(HttpListenerContext context,CancellationToken cancellationToken){
		// this just handels stuff
		context.Response.ContentEncoding = Encoding.UTF8;
		context.Response.ContentType = "text/html";
		if(context.Request.HttpMethod == "POST"){
			await HandelPostBook(context,cancellationToken);
			return;
		}
		await HandelGetBook(context,cancellationToken);
	}
	private static bool HandelPostIndex(HttpListenerContext context,ref string document){
		// true -> show the menu agian
		// false -> don't show it agian
		var hidParam = Servicer.GetHiddenParameters(context.Request);
		// try login to validate
		bool retVal = service.TryLogin(hidParam);
		if(!retVal){
			document += "Falsches Passwort oder E-Mail addresse!";
			//goto ACCOUNT_POST_END;
			return true;
		}
		string comment = hidParam["com"];
		string rateString = hidParam["rate"];
		string eMail = hidParam["mail"];
		int rateing;
		if(!int.TryParse(rateString,out rateing)){
			document += $"Leider ist {rateString} Keine Zahl...";
			return true;
		}
		// check if in rating range
		if(rateing < 1 || rateing > 5){
			document += $"Eine Zahl zwischen 1 und 5 (inklusive) erwartet.";
			return false;
		}
		// insert rating
		MySqlCommand cmd = new MySqlCommand($"REPLACE INTO Bewertung(Kunden_ID,Sterne,Nachricht) VALUES ((SELECT Kunden_ID FROM Kunden WHERE eMail = \"{eMail}\"),\"{rateing}\",\"{comment}\")",service.con);
		try{
			cmd.ExecuteNonQuery();
		}catch{document += "<br>Sie haben schon hier bewertet. ";}
		//}catch(Exception e){Console.WriteLine(e.ToString());}
		cmd.Dispose();
		return false;
	}
	private static async Task HandelHttpIndex(HttpListenerContext context,CancellationToken cancellationToken){
		try{
		string document = Program.docStart;
		MySqlCommand cmd = new MySqlCommand("",service.con);
		document += "<h1>Willkommen im Hotel Transelvanien</h1>";
		// querying bewertungen
		cmd.CommandText = "SELECT AVG(Sterne) FROM Bewertung";
		MySqlDataReader rdr = cmd.ExecuteReader();
		if(rdr.Read()){
			// if contains any rating
			if(!rdr.IsDBNull(0)){
				//document += $"Noch niemand hat hier eine Bewertung abgegeben. ";
				document += $"Unsere Kunden Zufiredenheit liegt bei {rdr.GetFloat(0):f1} Sternen. ";
			}
		}
		rdr.Dispose();
		rdr.Close();
		// also add links
		document += "<h2>Hier finden Sie beste Zimmer</h2>" +
			"<a href=\"/book\">Buchen</a> Sie jetzt bei uns, und finden Sie unsere schönsten Räume.<br> Unsere Räume sind riesig und preis wert.<br><a href=\"/book\">Buchen Sie bei uns!</a>";
		document += "<h2>Hier finden Sie bestes Essen</h2>" +
			"Unser <a href=\"/food\">Restaurante</a> hat fuer Sie zwischen 11:00 bis 18:00 offen!<br>Zu dem ist unser Essen super lecker und guenstig!<br><a href=\"/food\">Reservieren Sie sich einen Tisch Hier!</a>";
		document += "<h2>Hier finden Sie bestes Service</h2>" +
			"Unser Service ist das beste welches es gibt.<br>Wenn unser Service Ihnen nicht passt lassen Sie eine Bewertung dar.";
		bool isPost = context.Request.HttpMethod == "POST";
		bool handel = false;
		if(isPost)handel = HandelPostIndex(context,ref document);
		// insert rating form, if didn't rate recently
		if(!isPost || handel)
			document +=
"Bewerten Sie auch!"+
"<form method=\"post\" role=\"form\" action=\"/\">" +
"	<label for=\"rating\">Sterne-Bewertung:</label>" +
"	<input type=\"number\" id=\"rating\" name=\"rate\" min=\"1\" max=\"5\"></input><br>" +
"	<label for=\"comment\">Nachricht:</label>" +
"	<textarea id=\"comment\" name=\"com\" rows=\"4\" cols=\"50\"></textarea><br>" +
"	<label for=\"e-mail\">E-Mail:</label>" +
"	<input type=\"email\" id=\"e-mail\" name=\"mail\"></input><br>" +
"	<label for=\"pwd\">Passwort:</label>" +
"	<input type=\"password\" id=\"pwd\" name=\"pwd\"></input><br>" +
"	<input type=\"submit\" value=\"submit\">" +
"</form>";
		cmd.CommandText = "SELECT CONCAT(k.VorName,' ',k.NachName),b.Sterne,b.Nachricht FROM Bewertung b JOIN Kunden k ON k.Kunden_ID = b.Kunden_ID ORDER BY rand() LIMIT 5";
		document += "Hier sind weitere Bewertungen:<br>";
		rdr = cmd.ExecuteReader();
		// insert 5 Ratings here
		while(rdr.Read()){
			string msg = rdr.GetString(2);
			msg = Servicer.DecodeEscaped(msg);
			document += $"<div>Von:{Servicer.DecodeEscaped(rdr.GetString(0))} mit {rdr.GetInt32(1)} Sternen: {msg}</div>";
		}
		rdr.Dispose();
		rdr.Close();
		//
		cmd.Dispose();
		//HandelGetIndex(context,ref document);
		document += Program.docEnd;
		var bytes = Encoding.UTF8.GetBytes(document);
		await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
		}catch(Exception e){Console.WriteLine(e.ToString());}
	}
}
