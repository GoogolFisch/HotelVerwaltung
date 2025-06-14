using MySql.Data.MySqlClient;
using LightHTTP;
using System.ComponentModel;
using LightHTTP.Handling;
using LightHTTP.Internal;
using System;
using System.IO;
using System.Web;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

public class Servicer{
	// random token string data
	public static readonly string tokenRandomData = "stringRandomData" + Random.Shared.Next(1,10000);
	// mysql connection string
	public const string sqlServerString = @"server=localhost;userid=hotelServer;password=1234;database=hotelServer";
	// date formats
	public const string yyyymmdd = "yyyy-MM-dd";
	public const string ddmmyyyy = "dd.MM.yyyy";
	public const string hhmmss = "HH:mm:ss";
	public LightHttpServer server;// = new LightHttpServer(); // https://github.com/javidsho/LightHTTP.git
	public MySqlConnection con;
	public string webServerUrl {private set; get;}
	public List<string> existingTables;
	public List<RoomInfos> roomTypes;
	public bool CheckTableExists(string tableName){
		return existingTables.Contains(tableName.ToLower());
	}
	// just checks the table format to exactly match the given one
	public bool EnsureTableFormat(string tableName,string formatStr)
	{
		MySqlCommand cmd;
		tableName = Servicer.Sanitise(tableName);
		//string sql = $"DESCRIBE {tableName}";
		string sql = $"SHOW CREATE TABLE {tableName}";
		if(!CheckTableExists(tableName)){
			// auto create table
			Console.WriteLine("No Table - Creating one!");
			// the string is already the create table statement
			sql = $"{formatStr}";
			Console.WriteLine(sql);
			cmd = new MySqlCommand(sql,con);
			cmd.ExecuteNonQuery();
			cmd.Dispose();
			return false;
		}
		cmd = new MySqlCommand(sql, con);

		MySqlDataReader rdr = cmd.ExecuteReader();
		string tabLayout = "";
		if(rdr.Read())
			tabLayout = rdr.GetString(1);
		// "normalise the input"
		tabLayout = tabLayout.Replace("\n","");
		tabLayout = tabLayout.Replace("  "," ");
		// remove misc info, which will ruin us
		tabLayout = tabLayout.Substring(0,1 + tabLayout.LastIndexOf(")"));
		//Console.WriteLine(tabLayout);
		rdr.Dispose();
		cmd.Dispose();

		
		// checking if they match
		if(formatStr.Trim().ToLower() == tabLayout.Trim().ToLower())
			return true;
		// dump diffrence to the programmer
		Console.WriteLine($"The expected format: {tableName}");
		Console.WriteLine(formatStr);
		Console.WriteLine("But got");
		Console.WriteLine(tabLayout);

		// didn't match
		return false;
	}

	// remove all potentially dangerous characters from an string!
	public static string Sanitise(string inStr){
		string outStr = "";
		foreach(char inChar in inStr){
			// keep spaces
			if(inChar == ' ')
				outStr += inChar;
			// prevent XSS
			else if(inChar == '<')
				outStr += "&lt;";
			else if(inChar == '>')
				outStr += "&gt;";
			else if(inChar == '&')
				outStr += "&amp;";
			// keep every character above the ' or the "
			else if(inChar >= '(')// && inChar <= '~')
				outStr += inChar;
			else{
				// escape the character with the byte encoding
				byte[] charCode = Encoding.UTF8.GetBytes(new char[]{inChar});
				foreach(byte chh in charCode)
					// each char in hex
					outStr += $"%{chh:X2}";
			}
		}
		return outStr.Trim();
	}
	// decode into readable text, keep < > & sanitised
	public static string DecodeEscaped(string hexString){
		byte[] asBytes = new byte[4];
		int byteIndex = 0;
		string strOut = "";
		for(int stringIndex = 0;stringIndex < hexString.Length;stringIndex++){
			// everything above ' or " is safe
			if(hexString[stringIndex] >= '(')
				strOut += hexString[stringIndex];
			// space is save
			else if(hexString[stringIndex] == ' ')
				strOut += hexString[stringIndex];
			// also keep &, for the XSS prevention (and displaying)
			else if(hexString[stringIndex] == '&')
				strOut += hexString[stringIndex];
			else{
				// me trying to decode the escaped chars
				while(hexString[stringIndex] == '%'){
					stringIndex++;
					// upper half
					int part = hexString[stringIndex] - '0';
					if(part > 10){
						part += 10 - 'A' + '0';
					}
					part <<= 4;
					// save into the byte array
					asBytes[byteIndex / 2] = (byte)part;
					stringIndex++;
					// lower half
					part = (byte)hexString[stringIndex] - '0';
					if(part > 10){
						part += 10 - 'A' + '0';
					}
					// save into the byte array
					asBytes[byteIndex >> 1] |= (byte)part;
				}
				// encode to outString
				strOut += System.Text.Encoding.UTF8.GetString(asBytes, 0, byteIndex >> 1);
			}
		} // foreach loop
		// output string
		return strOut;
	}
	
        public static int GetOpenPort()
        {
		// stolen from LichtHTTP
		var listener = new TcpListener(IPAddress.Loopback, 0);
		try
		{
			listener.Start();
			return (listener.LocalEndpoint as IPEndPoint)!.Port;
		}
		finally
		{
			listener.Stop();
		}
        }
	// print all ip addresses
	public static void GetAllIpStrings(int port)
	{
		// stolen from LichtHTTP?
		var host = Dns.GetHostEntry(Dns.GetHostName());
		//string hostName = "";
		foreach (var ip in host.AddressList)
		{
			if (ip.AddressFamily == AddressFamily.InterNetwork){
				// print posible ip's with ports
				Console.WriteLine($"http://{ip.ToString()}:{port}");
			}
		}
	}
	// 
	public Servicer(){
		// MySql connection
		con = new MySqlConnection(sqlServerString);
		// catch if it is null
		if(con is null)
			throw new Exception("could not connect");
		// test connectivity to the MySQL server
		try{
			con.Open();
		}
		catch (MySqlException e){
			// if error is known
			if(e. Message.Equals("Unable to connect to any of the specified MySQL hosts")){
				Console.Error.WriteLine("You need to start mysql under XAMPP");
			}
			else
				Console.Error.WriteLine(e.ToString());
			// just exit because it shouldn't continue
			Environment.Exit(1);
		}
		// initialise Web-Server
		server = new LightHttpServer();
		
		// in future move to port 80 or 443
		// or not, because of XAMPP
		var port = Servicer.GetOpenPort();
		var prefix = $"http://*:{port}/"; // connect to any ip
		// print the ips
		Servicer.GetAllIpStrings(port);
		server.Listener.Prefixes.Add(prefix);
		// save the ip, IDK
		webServerUrl = prefix;

		// load "static" tables
		ReadAllTalbes();
		// ???
		//serverStartTime = DateTime.Now;
	}
	// update / setup rooms
	public void ReadAllRoomTypes(){
		var cmd = new MySqlCommand();
		cmd.Connection = con;
		// IDK XXX
		cmd.CommandText = "SELECT RaumTyp,Kosten,AnzBetten FROM RaumTypen";
		MySqlDataReader rdr = cmd.ExecuteReader();
		roomTypes = new List<RoomInfos>();
		// insert all rooms
		while(rdr.Read()){
			//roomTypes.Add(rdr.GetString(0));
			roomTypes.Add(new RoomInfos(
					rdr.GetString(0),
					rdr.GetDecimal(1),
					rdr.GetInt32(2)
					));
		}
		cmd.Dispose();
		rdr.Dispose();
	}
	private void ReadAllTalbes(){
		// get existing talbes
		var cmd = new MySqlCommand();
		cmd.Connection = con;
		cmd.CommandText = "SHOW TABLES";
		MySqlDataReader rdr = cmd.ExecuteReader();
		existingTables = new List<string>();
		// get name of tables
		while(rdr.Read()){
			existingTables.Add(rdr.GetString(0).ToLower());
		}
		cmd.Dispose();
		rdr.Dispose();
	}
	// read the form stream
	public static String GetInputStream(HttpListenerRequest req){
		// if has any
		if(!req.HasEntityBody)
			return "";
		Stream body = req.InputStream;
		StreamReader reader = new StreamReader(body,req.ContentEncoding);
		String str = reader.ReadToEnd();
		reader.Close();

		body.Close();
		body.Dispose();
		return str;
	}
	// convert form string into dict
	// thank you random Website...
	public static Dictionary<String,String> GetHiddenParameters(HttpListenerRequest req){
		Dictionary<String,String> lookup = new Dictionary<String,String>();
		String data = GetInputStream(req);
		
		// The ParseQueryString method will parse the query string and return a NameValueCollection
		var paramsCollection = HttpUtility.ParseQueryString(data);

		// The foreach loop will iterate over the params collection and print the key and value for each param
		foreach (var key in paramsCollection.AllKeys)
		{
			//Console.WriteLine($"Key: {key} => Value: {paramsCollection[key]}");
			// I don't like to sanitise it more...
			lookup.Add(key,Sanitise(paramsCollection[key]));
		}
		return lookup;
    }

	// start web-server
	public void Start(){
		// start the web-server

		ReadAllRoomTypes();
		// do windows only stuff
		bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		if (isWindows)
			ShellHelper.RegisterHttp(webServerUrl);
		server.Start();
	}
	public void Stop(){
		// kill Webserver
		server.Dispose();
		// kill MySql
		con.Close();
	}
	// encode passwords
	public static string EncodePassword(string input){
		SHA256 sha256 = SHA256.Create();
		byte[] data = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
		sha256.Dispose();
		string coded = Convert.ToBase64String(data,0,data.Length);
		coded = coded.Replace('/','-');
		return coded;
	}
	// if user exists, then don't
	public int TryRegisterUser(Dictionary<string,string> lookup){
		string fName = lookup["fname"];
		string lName = lookup["lname"];
		string eMail = lookup["mail"];
		string birth = lookup["birth"];
		string pwd = EncodePassword(lookup["pwd"]);
		// if any null or "", stop
		if(fName is null || fName == "") return 2;
		if(lName is null || lName == "") return 2;
		if(eMail is null || eMail == "") return 2;
		if(birth is null || eMail == "") return 2;
		if(pwd is null || eMail == "") return 2;
		// unique eMail
		string sql = $"SELECT * FROM Kunden WHERE eMail = \"{eMail}\"";
		MySqlCommand cmd = new MySqlCommand(sql,con);
		MySqlDataReader pref = cmd.ExecuteReader();
		//Console.WriteLine(Program.ConcatAllTypes(pref));
		bool gotContent = pref.HasRows;
		// close this before!
		pref.Dispose();
		pref.Close(); 
		// user unique, then insert
		if(!gotContent){
			cmd.CommandText = "INSERT INTO Kunden" + 
				"(VorName,NachName,ErstellungsDatum," +
				"eMail,GeborenAm,password) VALUES (" +
				$"\"{fName}\"," + 
				$"\"{lName}\"," + 
				$"\"{DateTime.Now.ToString($"{Servicer.yyyymmdd} {Servicer.hhmmss}")}\"," + 
				$"\"{eMail}\"," + 
				$"\"{birth}\"," + 
				$"\"{pwd}\"" + 
					")";
			//Console.WriteLine($"{cmd.CommandText}");
			cmd.ExecuteNonQuery();
		}
		cmd.Dispose();
		return gotContent ? 1 : 0; // 0 -> succ | 1 -> fail
	}
	//
	public bool TryLogin(Dictionary<string,string> lookup){
		string eMail = lookup["mail"];
		string pwd = EncodePassword(lookup["pwd"]);
		if(eMail is null || eMail == "") return false;
		if(pwd is null || eMail == "") return false;
		// get Client with the unique eMail
		string sql = $"SELECT password FROM Kunden WHERE eMail = \"{eMail}\"";
		MySqlCommand cmd = new MySqlCommand(sql,con);
		MySqlDataReader pref = cmd.ExecuteReader();
		bool correct = false;
		string foundPwd = "";
		if(pref.HasRows){
			pref.Read();
			foundPwd = pref.GetString(0);
			// check hashed passwords
			if(foundPwd == pwd)
				correct = true;
		}
		pref.Dispose();
		pref.Close(); 
		cmd.Dispose();
		return correct;
	}
	// used for account stuff
	public string GetTokenFor(string accountEMail){
		// get client
		MySqlCommand cmd = new MySqlCommand($"SELECT password,Kunden_ID FROM Kunden WHERE eMail = \"{accountEMail}\"",con);
		MySqlDataReader pref = cmd.ExecuteReader();
		pref.Read();
		// create the token
		string base64Hash = $"{pref.GetString(0)}-{accountEMail}-{DateTime.Now.ToString(Servicer.yyyymmdd)}-{tokenRandomData}";
		int accountId = pref.GetInt32(1);
		pref.Close();
		pref.Dispose();
		cmd.Dispose();
		// reusing the password hasher, to get the real token
		// hoping for no collisions
		base64Hash = Servicer.EncodePassword(base64Hash);
		// the token format
		return $"{accountId}-{base64Hash}";
	}
	// same as before, only with the id
	public string GetTokenFor(int accountId){
		// get client
		MySqlCommand cmd = new MySqlCommand($"SELECT password,eMail FROM Kunden WHERE Kunden_ID = {accountId}",con);
		MySqlDataReader pref = cmd.ExecuteReader();
		pref.Read();
		// create the token
		string base64Hash = $"{pref.GetString(0)}-{pref.GetString(1)}-{DateTime.Now.ToString(Servicer.yyyymmdd)}-{tokenRandomData}";
		pref.Close();
		pref.Dispose();
		cmd.Dispose();
		// reusing the password hasher, to get the real token
		// hoping for no collisions
		base64Hash = Servicer.EncodePassword(base64Hash);
		// the token format
		return $"{accountId}-{base64Hash}";
	}
	// regenerate the Token, check with given one
	public bool CheckToken(string token,out int accountId){
		// get account id
		string accountStr = token.Split('-')[0];
		if(!int.TryParse(accountStr,out accountId))
			return false;
		// regenerate
		string refrence = GetTokenFor(accountId);
		// and check
		if(refrence != token)
			return false;
		// success!
		return true;
	}
	// get all bookings, used in "/account"
	public List<BookingInfo> GetBookingFromKundenID(int accId){
		MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Buchungen WHERE Kunden_ID = {accId}",con);
		MySqlDataReader pref = cmd.ExecuteReader();
		List<BookingInfo> bkInfos = new List<BookingInfo>();
		// you can't have 2 SQL Querys running at the same time!
		while(pref.Read()){
			bkInfos.Add(new BookingInfo(
				pref.GetInt32(0),
				pref.GetInt32(1),
				pref.GetDateTime(2),
				pref.GetDateTime(3),
				pref.GetDateTime(4)
						));
		}

		pref.Dispose();
		pref.Close();
		cmd.Dispose();
		return bkInfos;
	}
}
