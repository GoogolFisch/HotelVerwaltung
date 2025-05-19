var accInfo,innerData;

function accountStartEdit(){
	accInfo = document.getElementById("account");
	innerData = accInfo.innerHTML;
	let dat = innerData.split("<br>");
	let names = dat[0].split(" ");
	let firstName = names[1];
	let lastName = names[2];
	let email = dat[1].split(": ")[1];
	let birth = dat[2].split(": ")[1];
	console.log(firstName + " " + lastName + "\n" + email + " " + birth);
	accInfo.innerHTML = `<form method="post">
		<label for="fname">Vorname:</label>
		<input type="text" id="fname" name="fname" value="${firstName}"></input><br>
		<label for="lname">Nachname:</label>
		<input type="text" id="lname" name="lname" value="${lastName}"></input><br>
		<label for="mail">E-Mail:</label>
		<input type="text" id="mail" name="mail" value="${email}"></input><br>
		<label for="birth">E-Mail:</label>
		<input type="text" id="birth" name="birth" value="${birth}"></input><br>
		<button>Veraender!</button>
		</form>
		<button onclick=\"accountCancelEdit()\">Abbrechen!</button>`;
}

function accountCancelEdit(){
	accInfo.innerHTML = innerData;

	//window.location.reload();
}
