
public struct RoomInfos{
	string typeName;
	decimal cost;
	string picturePath;
	public RoomInfos(string typ,decimal cost){
		this.typeName = typ;
		this.cost = cost;
		this.picturePath = $"./images/{typ}.jpg";
	}
	public string ToHtml(){
		return
			$"<img src=\"{picturePath}\">" +
			"<div>" +
			$"Typ:{typeName}<br>" +
			// I know $ 150.00 is not €, but who cares!
			$"Kosten:{cost.ToString("C2")}<br>" +
			"</div><div>" +
			// adding events!
			$"<a class=\"big-select\" id=\"add-{typeName}\">+</a>" +
			$"<a class=\"big-select\" id=\"shw-{typeName}\">0</a>" +
			$"<a class=\"big-select\" id=\"sub-{typeName}\">-</a>" +
			"</div>"
			;
	}
}
