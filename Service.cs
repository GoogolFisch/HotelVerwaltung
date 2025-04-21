using MySql.Data.MySqlClient;
using LightHTTP;
using System.Text;
using System.ComponentModel;

public class Servicer{

	public const string sqlServerString = @"server=localhost;userid=hotelServer;password=1234;database=hotelServer";
	public LightHttpServer server;// = new LightHttpServer(); // https://github.com/javidsho/LightHTTP.git
	//public bool shouldKillAll;// = false;
	//public DateTime serverStartTime;// = DateTime.Now;
	public MySqlConnection con;
	public string webServerUrl {private set; get;}
	public List<string> existingTables;
	public bool CheckTableExists(string tableName){
		return existingTables.Contains(tableName);
	}
	public bool EnsureTableFormat(string tableName,string formatStr)
	{
		tableName = Servicer.Sanitise(tableName);
		if(!CheckTableExists(tableName)){
			Console.WriteLine("No Table!");
			return false;
		}
		string sql = $"DESCRIBE {tableName}";
		var cmd = new MySqlCommand(sql, con);

		MySqlDataReader rdr = cmd.ExecuteReader();
		string tabLayout = "";
		while (rdr.Read())
		{
			tabLayout += $"{rdr.GetString(0)} {rdr.GetString(1)}|";
			Console.WriteLine("{0} {1}", rdr.GetString(0),rdr.GetString(1));
		}
		rdr.Dispose();
		cmd.Dispose();

		if(formatStr == tabLayout)
			return true;
		Console.WriteLine("The expected format did't match the current one");
		Console.WriteLine("The expected format:");
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
	public Servicer(){
		// MySql connection
		con = new MySqlConnection(sqlServerString);
		con.Open();
		if(con is null)
			throw new Exception("could not connect");
		// initialise Web-Server
		server = new LightHttpServer();
		webServerUrl = server.AddAvailableLocalPrefix();
		Console.WriteLine(webServerUrl);

		// get existing talbes
		var cmd = new MySqlCommand();
		cmd.Connection = con;
		cmd.CommandText = "SHOW TABLES";
		MySqlDataReader rdr = cmd.ExecuteReader();
		existingTables = new List<string>();
		while(rdr.Read()){
			existingTables.Add(rdr.GetString(0));
		}
		cmd.Dispose();
		rdr.Dispose();
		// ???
		//serverStartTime = DateTime.Now;
	}

	public void Start(){
		// start the web-server
		server.Start();
	}
	public void Stop(){
		// kill Webserver
		server.Dispose();
		// kill MySql
		con.Close();
	}
}
