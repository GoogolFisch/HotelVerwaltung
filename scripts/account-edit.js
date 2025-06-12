var accInfo,innerData;

function accountStartEdit(){
	// get info from a div?
	accInfo = document.getElementById("account");
	innerData = accInfo.innerHTML;
	// doing string maipulation to get string data
	let dat = innerData.split("<br>");
	let email = dat[1].split(": ")[1];
	let names = dat[0].split(" ");
	let firstName = names[1];
	let lastName = names[2];
	let birth = dat[2].split(": ")[1].split(".");
	// convert int other format
	birth.reverse();
	birth = birth.join("-");
	console.log(firstName + " " + lastName + "\n" + email + " " + birth);
	// insert as standert values
	accInfo.innerHTML = `<form method="post">
		<label for="fname">Vorname:</label>
		<input type="text" id="fname" name="fname" value="${firstName}" required></input><br>
		<label for="lname">Nachname:</label>
		<input type="text" id="lname" name="lname" value="${lastName}" required></input><br>
		<label for="mail">E-Mail:</label>
		<input type="email" id="mail" name="mail" value="${email}" required></input><br>
		<label for="birth">E-Mail:</label>
		<input type="date" id="birth" name="birth" value="${birth}" required></input><br>
		<label for="tpwd">altes Password</label>
		<input type="password" id="tpwd" name="pwd" required></input><br>
		<label for="n1pwd">neues Password</label>
		<input type="password" id="n1pwd" name="npwd"></input><br>
		<label for="n2pwd">erneut Password</label>
		<input type="password" id="n2pwd" name="n2pwd"></input><br>
		<button>Veraender!</button>
		</form>
		<button onclick=\"accountCancelEdit()\">Abbrechen!</button>`;
}
function accountDeletionStart(){
	accInfo = document.getElementById("account");
	innerData = accInfo.innerHTML;
	// get email from the div
	let dat = innerData.split("<br>");
	let email = dat[1].split(": ")[1];
	let startDel = document.getElementById("deletionForm");
	// create a form, which ask the password.
	startDel.innerHTML = `<form method="post" action="${window.location.pathname}/delete-account">
		<!--<label for="mail">E-Mail:</label>-->
		<input type="hidden" id="mail" name="mail" value="${email}"></input><br>
		<label for="tpwd">Passwort zum löschen:</label>
		<input type="password" id="tpwd" name="pwd" required></input><br>
		<button>Löschen!</button>
		</form>
		Alle stornierbaren Buchungen werden mit dieser Aktion auch storniert!<br>
		Vorherige Buchungen werden gespeichert, in einer anonymisierten Form.<br>
		<button onclick=\"document.getElementById('deletionForm').innerHTML = '';\">Abbrechen!</button>`;
}


function cancelBooking(booking){
	// ask if to cancel
	if(!window.confirm("Buchung stornieren?"))
		return;
	window.location.href += "/storno-" + booking;
	console.log("Hello " + booking);
}
