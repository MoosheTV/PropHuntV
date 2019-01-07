var _serverCfg = {};
var _user = {};
var timer = 0;
$(document).ready(function() {
	window.setInterval(function() {
		var countdown = $('#countdown');
		var time = parseInt(countdown.data("time"));
		if(time > 0) {
			countdown.data("time", --time);

			var min = parseInt(Math.floor(time / 60) % 60);
			var sec = time % 60;
			countdown.text((min < 10 ? "0"+min : min)+":"+(sec < 10 ? "0"+sec : sec));
		} else if(countdown.is(":visible")) {
			$('.countdown').fadeOut(200);
		}
	}, 1000);

	window.addEventListener('message', function(e) {
		var event = e.data;
		var item = event.EventData;

		switch(event.EventName) {
			case "Lobby.PlayerCount":
				$('#playercount').text(item["PlayerCount"]+" Online");
				return;
			case "Lobby.ShowPlayerCount":
				$('#playercount').addClass("opaque");
				return;
			case "Lobby.HidePlayerCount":
				if(!$('#lobby-manager').is(":visible"))
					$('#playercount').removeClass("opaque");
				return;
			case "Lobby.Notification":
				var notice = $('#notification');
				notice.html(item["Message"]);
				notice.addClass("opaque");
				window.setTimeout(function() {
					$('#notification').removeClass("opaque");
				}, item["Seconds"] * 1000);
				return;
			case "Lobby.Open":
				$('#lobby-manager').fadeIn(1000);
				$('#playercount').addClass("opaque");
				return;
			case "Lobby.Close":
				$('#lobby-manager').fadeOut(1000);
				$('#playercount').removeClass("opaque");
				return;
			case "Lobby.Countdown":
				console.log(item);
				startCountdown(item["Message"], item["Seconds"]);
				return;
			case "Lobby.Config":
				_serverCfg = item;
				$('#character-selection').empty();
				for(i in item["Characters"]) {
					var c = item["Characters"][i];
					var chara = $('<div class="character" style="background-image: url(\'headshots/'+c.ModelName+'.png\')" id="character-'+c.ModelName+'" data-cost="'+c.PointCost+'"></div>');
					$('#character-selection').append(chara);
					if(_user["PedModel"] == c.ModelName || item["DefaultPedModel"] == c.ModelName) {
						setActiveChar(chara);
					}
					if(c["Unlocked"] || _user["Unlockables"].includes(c.ModelName)) {
						chara.addClass("unlock");
					}
					chara.on("click", function() {

						if(!$(this).hasClass("unlock") && _user.Points >= parseInt($(this).data("cost"))) {
							sendNuiEvent("Lobby.Purchase", {
								"Item": $(this).attr("id").substring(10)
							});
						}

						if(!$(this).hasClass("selected")) {
							sendNuiEvent("Lobby.SwitchModel", {
								"Model": $(this).attr("id").substring(10)
							})
							return;
						}
					});

					var price = $('<p class="price" data-cost="'+c.PointCost+'">'+formatNumber(c.PointCost)+'p</p>')
					if(c.PointCost < _user.Points) {
						price.addClass("right-price");
					}
					if(c["Unlocked"] || _user["Unlockables"].includes(c["ModelName"])) {
						price.hide();
					}
					chara.append(price);
					if(_user["PedModel"] != undefined) {
						setActiveChar($('#character-'+_user.PedModel));
					}
				}
				return;
			case "Lobby.UserData":
				_user = item;
				setActiveChar($('#character-'+_user.PedModel));
				for(c in item["Unlockables"]) {
					$('#character-'+c).addClass("unlock");
				}
				$('#info').text(item["Points"]+" Points")
				return;
		}
	});
	$('#quit').on("click", function() {
		$('.quit-container').fadeIn(250);
	});
	$('#quit-yes').on("click", function() {
		$('body').fadeOut(50);
		sendNuiEvent("Lobby.Quit");
	});
	$('#quit-no').on("click", function() {
		$('.quit-container').fadeOut(250);
	});

	$('#customize').on("click", function() {
		setActive($(this));
		sendNuiEvent("Lobby.SwitchCamera", {"camera": "custom"});
	});
	$('#player').on("click", function() {
		setActive($(this));
		sendNuiEvent("Lobby.SwitchCamera", {"camera": "main"});
	});
	$('#play').on("click", function() {
		$('#lobby-manager').fadeOut(1000);
		setActive($('.default'));
		sendNuiEvent("Lobby.Close");
	});
});

function startCountdown(msg, time) {
	if(time <= 0) {
		$('.countdown').hide();
		return;
	}
	$('#countdown').data("time", time);
	$('#countdown-message').text(msg);
	$('.countdown').show();
}

function currentActive() {
	var found = $('.lobby-top-row').find(".active");
	return found.length == 0 ? $("<div></div>") : $('#'+found[0].id);
}

function setActive(obj) {
	$("#"+currentActive().data("target")).hide();
	currentActive().removeClass("active");

	obj.addClass("active");
	$("#"+obj.data("target")).show();
}

function currentActiveChar() {
	var found = $('#character-selection').find(".selected");
	return found.length == 0 ? $("<div></div>") : $('#'+found[0].id);
}

function setActiveChar(obj) {
	currentActiveChar().removeClass("selected");
	obj.addClass("selected");
}

function sendNuiEvent(name, data={}) {
	$.post("http://prophunt/"+name, JSON.stringify(data));
}

function formatNumber(num, decimals=0) {
	return num.toFixed(decimals).replace(/\d(?=(\d{3})+\.)/g, '$&,');
}
