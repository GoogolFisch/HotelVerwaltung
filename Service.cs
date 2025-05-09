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
using System.Runtime.InteropServices;

public class Servicer{

	public const string sqlServerString = @"server=localhost;userid=hotelServer;password=1234;database=hotelServer";
	public LightHttpServer server;// = new LightHttpServer(); // https://github.com/javidsho/LightHTTP.git
	//public bool shouldKillAll;// = false;
	//public DateTime serverStartTime;// = DateTime.Now;
	public MySqlConnection con;
	public string webServerUrl {private set; get;}
	public List<string> existingTables;
	public bool CheckTableExists(string tableName){
		return existingTables.Contains(tableName.ToLower());
	}
	public bool EnsureTableFormat(string tableName,string formatStr)
	{
		MySqlCommand cmd;
		tableName = Servicer.Sanitise(tableName);
		string sql = $"DESCRIBE {tableName}";
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
			sql = $"CREATE TABLE {tableName}({formatStr});";
			Console.WriteLine(sql);
			cmd = new MySqlCommand(sql,con);
			cmd.ExecuteNonQuery();
			cmd.Dispose();
			return false;
		}
		cmd = new MySqlCommand(sql, con);

		MySqlDataReader rdr = cmd.ExecuteReader();
		string tabLayout = "";
		bool isFirst = true;
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
			/*if(rdr.GetString(1) == "date"){
				tabLayout += " DEFAULT CURRENT_TIMESTAMP";
			}*/
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
		}
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
			else if(inChar >= '0' && inChar <= '9')
				outStr += inChar;
			else if(inChar >= 'A' && inChar <= 'Z')
				outStr += inChar;
			else if(inChar >= 'a' && inChar <= 'z')
				outStr += inChar;
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
		// ???
		//serverStartTime = DateTime.Now;
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
	public bool TryRegisterUser(string fName, string lName, string pwd){
		fName = Servicer.Sanitise(fName);
		lName = Servicer.Sanitise(lName);
		string sql = $"SELECT * FROM Kunden WHERE VorName = \"{fName}\" AND NachName = \"{lName}\"";
		MySqlCommand cmd = new MySqlCommand(sql,con);
		MySqlDataReader pref = cmd.ExecuteReader();
		Console.WriteLine(Program.ConcatAllTypes(pref));
		pref.Dispose();
		pref.Close();
		return false;
	}
}
