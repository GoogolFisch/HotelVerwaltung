var roomTypes = new Map();

function room_update(roomTyp,val){
	let shwCnt = document.getElementById("shw-"+roomTyp);
	let cnt = Math.floor(shwCnt.text - 0);
	cnt += val;
	if(cnt < 0)cnt = 0;
	if(cnt > 5)cnt = 5; // this is an limit I choose
	shwCnt.text = cnt + "";
	shwCnt = document.getElementById("snd-"+roomTyp);
	shwCnt.value = cnt;
	
	total_update();
}

function total_update(){
	let roomKeys = roomTypes.keys().toArray();
	let shwCnt;
	let cost = 0;
	for(let ov = 0;ov < roomKeys.length;ov++){
		shwCnt = document.getElementById("shw-"+roomKeys[ov]);
		cost += (shwCnt.text - 0) * roomTypes.get(roomKeys[ov]);
	}
	shwCnt = document.getElementById("costing");
	shwCnt.innerHTML = "Kostet: $" + cost;
}
