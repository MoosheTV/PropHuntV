resource_type 'gametype' { name = 'PropHunt' }

client_scripts {
	'client/PropHuntV.Client.net.dll'
}
server_scripts {
	'server/PropHuntV.Server.net.dll'
}

files({
	'Newtonsoft.Json.dll',
	'html/index.html',
	'html/index.css',
	'html/index.js',
	'html/prophuntv.png',

	'html/headshots/Acult02AMY.png',
	'html/headshots/Babyd.png',
	'html/headshots/Beach01AFM.png',
	'html/headshots/Bodybuild01AFM.png',
	'html/headshots/Jesus01.png',
	'html/headshots/JoeMinuteman.png',
	'html/headshots/Juggalo01AFY.png',
	'html/headshots/Juggalo01AMY.png',
	'html/headshots/Mani.png',
	'html/headshots/MovAlien01.png',
	'html/headshots/Tourist01AMM.png',
	'html/headshots/TrafficWarden.png',
	'html/headshots/none.png'
})

ui_page 'html/index.html'
