var http = require("http");
var options = {
	host: 'www.google.fr',
	port: 80,
	path: '/',
	method: 'GET'
};

var req = http.get(options, function(res) {
	var pageData = "";
	res.setEncoding('utf8');
	res.on('data', function (chunk) {
		pageData += chunk;
	});

	res.on('end', function(){
		console.log(pageData);
	});
});
