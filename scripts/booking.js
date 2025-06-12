var roomTypes = new Map();
const ONE_DAY = 1000 * 60 * 60 * 24;

function room_update(roomTyp,val){
	// get room div
	let shwCnt = document.getElementById("shw-"+roomTyp);
	let cnt = Math.floor(shwCnt.text - 0);
	cnt += val;
	// limit
	if(cnt < 0)cnt = 0;
	if(cnt > 6)cnt = 6; // this is an limit I choose
	shwCnt.text = cnt + "";
	// store
	shwCnt = document.getElementById("snd-"+roomTyp);
	shwCnt.value = cnt;
	
	total_update();
}

function days_between(date1, date2) {
	date1 = Date.parse(date1);
	date2 = Date.parse(date2);
	// The number of milliseconds in one day
	// Calculate the difference in milliseconds
	const differenceMs = Math.abs(date1 - date2);
	// Convert back to days and return
	return Math.round(differenceMs / ONE_DAY);
}
function getISODate(time){
	// IDK, I want it short, to convert stuff into the ISO Date format
	let tt = new Date(time);
	return tt.toISOString().slice(0,10);
}

function total_update(){
	// get times
	let timeStart = document.getElementById("from");
	let timeEnd = document.getElementById("till");
	//timeStart.min = getISODate(Date.now() + ONE_DAY);
	// set auto limit
	timeEnd.min = getISODate(Date.parse(timeStart.value) + ONE_DAY);
	let dayCount = days_between(timeStart.value,timeEnd.value);
	// don't make a NaN
	if(dayCount === undefined)
		dayCount = 1;
	if(isNaN(dayCount))
		dayCount = 1;
	// count stuff up
	let roomKeys = roomTypes.keys().toArray();
	let shwCnt;
	let cost = 0;
	for(let ov = 0;ov < roomKeys.length;ov++){
		shwCnt = document.getElementById("shw-"+roomKeys[ov]);
		cost += (shwCnt.text - 0) * roomTypes.get(roomKeys[ov]);
	}
	// store into costs
	shwCnt = document.getElementById("costing");
	shwCnt.innerHTML = "Kostet: $" + cost * dayCount;
}

function room_focus(roomType){
	// remove selection
	let selectedBoxes = document.getElementsByClassName("rooms-select");
	let roomBox = document.getElementsByClassName("rooms-" + roomType)[0];
	for(let over = 0;over < selectedBoxes.length;){
		if(selectedBoxes[over] == roomBox)
			return;
		// this could be diffrent
		let extras = selectedBoxes[over].getElementsByClassName("extra-select")[0];
		console.log(extras);
		selectedBoxes[over].removeChild(extras);
		// this removes it from the list
		selectedBoxes[over].classList.remove("rooms-select");
	}
	// add selection to selected element.
	roomBox.classList.add("rooms-select");
	roomBox.innerHTML += `<div class="extra-select">Zur Buchung hinzufuegen.
<a class=\"big-select\" id=\"add-${roomType}\" onclick=\"room_update('${roomType}',1);\">+</a><a class=\"big-select\" id=\"sub-${roomType}\" onclick=\"room_update('${roomType}',-1);\">-</a> </div>`;
	console.log(roomBox);
}
