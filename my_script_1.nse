--����� ������� �.�.
--������ ��������������� �� ������� Apache Tomcat �� �������� "404"  ������ ������� Apache Tomcat


local http = require "http"

portrule = function(host, port)
	return true
end

action = function(host, port)
	local path = "/server-status"
	local response = http.get(host,port,path)
	return  string.match(response.body, "<h3>Apache Tomcat/([^<]*)</h3>")
end