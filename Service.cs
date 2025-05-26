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

	public static string tokenRandomData = "stringRandomData" + Random.Shared.Next(1,100);
	public const string sqlServerString = @"server=localhost;userid=hotelServer;password=1234;database=hotelServer";
	public const string yyyymmdd = "yyyy-MM-dd";
	public const string ddmmyyyy = "dd.MM.yyyy";
	public const string hhmmss = "HH:mm:ss";
	public LightHttpServer server;// = new LightHttpServer(); // https://github.com/javidsho/LightHTTP.git
	//public bool shouldKillAll;// = false;
	//public DateTime serverStartTime;// = DateTime.Now;
	public MySqlConnection con;
	public string webServerUrl {private set; get;}
	public List<string> existingTables;
	public List<RoomInfos> roomTypes;
	public bool CheckTableExists(string tableName){
		return existingTables.Contains(tableName.ToLower());
	}
	public bool EnsureTableFormat(string tableName,string formatStr)
	{
		MySqlCommand cmd;
		tableName = Servicer.Sanitise(tableName);
		//string sql = $"DESCRIBE {tableName}";
		string sql = $"SHOW CREATE TABLE {tableName}";
		if(!CheckTableExists(tableName)){
			Console.WriteLine("No Table - Creating one!");
			/*
			List<string> param = new List<string>(formatStr.Split("|"));
			param[0] += " PRIMARY KEY AUTO_INCREMENT";
			param.RemoveAt(param.Count - 1);
			for(int i = 1;i < param.Count;i++){
				param[i] += " NOT NULL";
				if(param[i].Contains("Datum")){
					param[i] += " DEFAULT CURRENT_TIMESTAMP";
				}
			}
			sql = $"CREATE TABLE {tableName}({String.Join(',',param)});";
			*/
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
		/*bool isFirst = true;
		List<string> primaryList = new List<string>();
		while (rdr.Read())
		{
			if(!isFirst)
				tabLayout += ",";
			else{
				//Console.WriteLine(Program.ConcatAllTypes(rdr));
				//Console.WriteLine(rdr.FieldCount);
				isFirst = false;
			}
			tabLayout += $"{rdr.GetString(0)} {rdr.GetString(1)}";
			//tabLayout += $" {rdr.GetString(4)} {rdr.GetString(5)}";
			if(rdr.GetString(3) == "PRI")
				primaryList.Add(rdr.GetString(0));
			else{
				if(rdr.GetString(3) != "")
					tabLayout += $" {rdr.GetString(3)}";
				if(rdr.GetString(2) == "NO")
					tabLayout += " NOT NULL";
			}
			for(int i = 5; i < rdr.FieldCount;i++)
				tabLayout += $" {rdr.GetString(i)}";
		}
		// populate primary keys
		if(primaryList.Count > 0){
			tabLayout += ",PRIMARY KEY (";
			isFirst = true;
			foreach(string keys in primaryList){
				if(!isFirst)tabLayout += ",";
				tabLayout += $"{keys}";
				isFirst = false;
			}
			tabLayout += ")";
		}*/
		if(rdr.Read())
			tabLayout = rdr.GetString(1);
		tabLayout = tabLayout.Replace("\n","");
		tabLayout = tabLayout.Replace("  "," ");
		tabLayout = tabLayout.Substring(0,1 + tabLayout.LastIndexOf(")"));
		//Console.WriteLine(tabLayout);
		rdr.Dispose();
		cmd.Dispose();

		
		if(formatStr.Trim().ToLower() == tabLayout.Trim().ToLower())
			return true;
		Console.WriteLine($"The expected format: {tableName}");
		Console.WriteLine(formatStr);
		Console.WriteLine("But got");
		Console.WriteLine(tabLayout);


		return false;
	}
	public static string GetTableData(System.Data.DataTable table)
	{
		string outing = "";
		foreach (System.Data.DataColumn col in table.Columns)
		{
			outing += $"{col.ColumnName}\t";
		}
		outing += "\n";
		foreach (System.Data.DataRow row in table.Rows)
		{
			foreach (System.Data.DataColumn col in table.Columns)
			{
				outing += $"{row[col]}\t";
			}
			outing += "<br>\n";
		}
		return outing;
	}
	// remove all potentially dangerous characters from an string!
	public static string Sanitise(string inStr){
		/*
		string outStr = inStr;
		foreach(char nogoChar in "/'.\"\\$"){
			outStr = outStr.Replace("" + nogoChar,"");
		}*/
		string outStr = "";
		foreach(char inChar in inStr){
			if(inChar == ' ')
				outStr += inChar;
			/*
			else if(strict > 0){
				// maybe add " -> %22 ...
				if(inChar == '!')
					outStr += inChar;
				else if(inChar >= '#' && inChar <= '&')
					outStr += inChar;
				else if(inChar >= '(' && inChar <= '_')
					outStr += inChar;
				else if(inChar >= 'a' && inChar <= '~')
					outStr += inChar;
				// idk what can happen here!
			}
			else if(inChar >= '0' && inChar <= '9')
				outStr += inChar;
			else if(inChar >= '@' && inChar <= 'Z')
				outStr += inChar;
			else if(inChar >= 'a' && inChar <= 'z')
				outStr += inChar;
				*/
			else if(inChar >= '(' && inChar <= '~')
				outStr += inChar;
			else{
				byte[] charCode = Encoding.UTF8.GetBytes(new char[]{inChar});
				foreach(byte chh in charCode)
					// each char in hex
					outStr += $"%{chh:X}";
			}
		}
		return outStr.Trim();
	}
	
        public static int GetOpenPort()
        {
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
	public static void GetAllIpStrings(int port)
	{
		var host = Dns.GetHostEntry(Dns.GetHostName());
		//string hostName = "";
		foreach (var ip in host.AddressList)
		{
			if (ip.AddressFamily == AddressFamily.InterNetwork){
				//hostName = ip.ToString();
				Console.WriteLine($"http://{ip.ToString()}:{port}");
			}
		}
		//throw new Exception("No network adapters with an IPv4 address in the system!");
	}
	public Servicer(){
		// MySql connection
		con = new MySqlConnection(sqlServerString);
		try{
			con.Open();
		}
		catch (MySqlException e){
			if(e. Message.Equals("Unable to connect to any of the specified MySQL hosts")){
				Console.Error.WriteLine("You need to start mysql under XAMPP");
			}
			else
				Console.Error.WriteLine(e.ToString());
			Environment.Exit(1);
		}
		if(con is null)
			throw new Exception("could not connect");
		// initialise Web-Server
		server = new LightHttpServer();
		
		// in future move to port 80 or 443
		var port = Servicer.GetOpenPort();
		var prefix = $"http://*:{port}/"; // connect to any ip
		Servicer.GetAllIpStrings(port);
		server.Listener.Prefixes.Add(prefix);
		webServerUrl = prefix;
		//webServerUrl = server.AddAvailableLocalPrefix();

		// load "static" tables
		ReadAllTalbes();
		// ???
		//serverStartTime = DateTime.Now;
	}
	private void ReadAllRoomTypes(){
		var cmd = new MySqlCommand();
		cmd.Connection = con;
		// this will change!
		cmd.CommandText = "SELECT RaumTyp,AVG(Kosten) FROM Raum GROUP BY RaumTyp";
		MySqlDataReader rdr = cmd.ExecuteReader();
		roomTypes = new List<RoomInfos>();
		while(rdr.Read()){
			//roomTypes.Add(rdr.GetString(0));
			roomTypes.Add(new RoomInfos(
					rdr.GetString(0),
					rdr.GetDecimal(1)
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
		while(rdr.Read()){
			existingTables.Add(rdr.GetString(0).ToLower());
		}
		cmd.Dispose();
		rdr.Dispose();
	}
	public static String GetInputStream(HttpListenerRequest req){
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
	public static String GetInputStream(HttpListenerRequest req){
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
	public static Dictionary<String,String> GetHiddenParameters(HttpListenerRequest req){
		Dictionary<String,String> lookup = new Dictionary<String,String>();
		String data = GetInputStream(req);
		
		// The ParseQueryString method will parse the query string and return a NameValueCollection
		var paramsCollection = HttpUtility.ParseQueryString(data);

		// The foreach loop will iterate over the params collection and print the key and value for each param
		foreach (var key in paramsCollection.AllKeys)
		{
			//Console.WriteLine($"Key: {key} => Value: {paramsCollection[key]}");
			lookup.Add(key,paramsCollection[key]);
		}
		return lookup;
	}

	public void Start(){
		// start the web-server

		ReadAllRoomTypes();
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
	public static string EncodePassword(string input){
		SHA256 sha256 = SHA256.Create();
		byte[] data = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
		sha256.Dispose();
		return Convert.ToBase64String(data,0,32);
	}
	public int TryRegisterUser(Dictionary<string,string> lookup){
		string fName = lookup["fname"];
		string lName = lookup["lname"];
		string eMail = lookup["mail"];
		string birth = lookup["birth"];
		string pwd = EncodePassword(lookup["pwd"]);
		if(fName is null || fName == "") return 2;
		if(lName is null || lName == "") return 2;
		if(eMail is null || eMail == "") return 2;
		if(birth is null || eMail == "") return 2;
		if(pwd is null || eMail == "") return 2;
		//string sql = $"SELECT * FROM Kunden WHERE VorName = \"{fName}\" AND NachName = \"{lName}\"";
		string sql = $"SELECT * FROM Kunden WHERE eMail = \"{eMail}\"";
		MySqlCommand cmd = new MySqlCommand(sql,con);
		MySqlDataReader pref = cmd.ExecuteReader();
		//Console.WriteLine(Program.ConcatAllTypes(pref));
		bool gotContent = pref.HasRows;
		// close this before!
		pref.Dispose();
		pref.Close(); 
		// 
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
	public bool TryLogin(Dictionary<string,string> lookup){
		string eMail = lookup["mail"];
		string pwd = EncodePassword(lookup["pwd"]);
		if(eMail is null || eMail == "") return false;
		if(pwd is null || eMail == "") return false;
		string sql = $"SELECT password FROM Kunden WHERE eMail = \"{eMail}\"";
		MySqlCommand cmd = new MySqlCommand(sql,con);
		MySqlDataReader pref = cmd.ExecuteReader();
		bool correct = false;
		string foundPwd = "";
		if(pref.HasRows){
			pref.Read();
			foundPwd = pref.GetString(0);
			if(foundPwd == pwd)
				correct = true;
		}
		pref.Dispose();
		pref.Close(); 
		cmd.Dispose();
		return correct;
	}
	public string GetTokenFor(string accountEMail){
		accountEMail = accountEMail;
		MySqlCommand cmd = new MySqlCommand($"SELECT password,Kunden_ID FROM Kunden WHERE eMail = \"{accountEMail}\"",con);
		MySqlDataReader pref = cmd.ExecuteReader();
		pref.Read();
		string base64Hash = $"{pref.GetString(0)}-{accountEMail}-{DateTime.Now.ToString(Servicer.yyyymmdd)}-{tokenRandomData}";
		int accountId = pref.GetInt32(1);
		pref.Close();
		pref.Dispose();
		cmd.Dispose();
		base64Hash = Servicer.EncodePassword(base64Hash);
		return $"{accountId}-{base64Hash}";
	}
	public string GetTokenFor(int accountId){
		MySqlCommand cmd = new MySqlCommand($"SELECT password,eMail FROM Kunden WHERE Kunden_ID = {accountId}",con);
		MySqlDataReader pref = cmd.ExecuteReader();
		pref.Read();
		string base64Hash = $"{pref.GetString(0)}-{pref.GetString(1)}-{DateTime.Now.ToString(Servicer.yyyymmdd)}-{tokenRandomData}";
		pref.Close();
		pref.Dispose();
		cmd.Dispose();
		base64Hash = Servicer.EncodePassword(base64Hash);
		return $"{accountId}-{base64Hash}";
	}
	public bool CheckToken(string token,out int accountId){
		string accountStr = token.Split('-')[0];
		if(!int.TryParse(accountStr,out accountId))
			return false;
		//
		string refrence = GetTokenFor(accountId);
		if(refrence != token)
			return false;
		// success!
		return true;
	}
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
