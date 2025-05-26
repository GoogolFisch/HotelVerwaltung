var accInfo,innerData;

function accountStartEdit(){
	accInfo = document.getElementById("account");
	innerData = accInfo.innerHTML;
	let dat = innerData.split("<br>");
	let names = dat[0].split(" ");
	let firstName = names[1];
	let lastName = names[2];
	let email = dat[1].split(": ")[1];
	let birth = dat[2].split(": ")[1].split(".");
	birth.reverse();
	birth = birth.join("-");
	console.log(firstName + " " + lastName + "\n" + email + " " + birth);
	accInfo.innerHTML = `<form method="post">
		<label for="fname">Vorname:</label>
		<input type="text" id="fname" name="fname" value="${firstName}"></input><br>
		<label for="lname">Nachname:</label>
		<input type="text" id="lname" name="lname" value="${lastName}"></input><br>
		<label for="mail">E-Mail:</label>
		<input type="email" id="mail" name="mail" value="${email}"></input><br>
		<label for="birth">E-Mail:</label>
		<input type="date" id="birth" name="birth" value="${birth}"></input><br>
		<label for="tpwd">altes Password</label>
		<input type="password" id="tpwd" name="pwd"></input><br>
		<label for="n1pwd">neues Password</label>
		<input type="password" id="n1pwd" name="npwd"></input><br>
		<label for="n2pwd">erneut Password</label>
		<input type="password" id="n2pwd" name="n2pwd"></input><br>
		<button>Veraender!</button>
		</form>
		<button onclick=\"accountCancelEdit()\">Abbrechen!</button>`;
}

function accountCancelEdit(){
	accInfo.innerHTML = innerData;

	//window.location.reload();
}


function cancelBooking(booking){
	if(!window.confirm("Buchung stornieren?"))
		return;
	window.location.href += "/storno-" + booking;
	console.log("Hello " + booking);
}
