
public struct RoomInfos{
	public string typeName;
	public decimal cost;
	public string picturePath;
	public RoomInfos(string typ,decimal cost){
		this.typeName = typ;
		this.cost = cost;
		this.picturePath = $"./images/{typ}.jpeg";
	}
	public string ToHtml(){
		return $"<li class=\"rooms rooms-{typeName}\" onclick=\"room_focus('{typeName}')\">" + 
			"<div class=\"flex-row\">" +
			$"<img src=\"{picturePath}\">" +
			"<div>" +
			$"Typ:{typeName}<br>" +
			// I know $ 150.00 is not €, but who cares!
			$"Kosten:{cost.ToString("C2")}<br>" +
			"</div><div>" +
			// adding events! moving them into an sub menu
			/*$"<a class=\"big-select\" id=\"add-{typeName}\" onclick=\"room_update('{typeName}',1);\">+</a>" +
			$"<a class=\"big-select\" id=\"sub-{typeName}\" onclick=\"room_update('{typeName}',-1);\">-</a>" + */
			$"<a class=\"big-select\" id=\"shw-{typeName}\" onclick=\"room_update('{typeName}',0);\">0</a>" +
			"</div></div>"+
			$"<script>roomTypes.set(\"{typeName}\",{cost});</script>" +
			"</li>";
	}
}
