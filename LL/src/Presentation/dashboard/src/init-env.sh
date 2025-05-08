# init-env.sh
#!/bin/sh
# This script is executed when the docker image is started


# create env.js with current environment variables
envsubst < /usr/share/nginx/html/assets/env.template.js > /usr/share/nginx/html/assets/env.js

# to prevent the browser from using an old cached version of env.js we add a checksum to the filename
# replace reference in index.html
sed -i "s/\/assets\/env.*\.js/\/assets\/env.$(sha1sum /usr/share/nginx/html/assets/env.js | cut -d' ' -f1).js/g" /usr/share/nginx/html/index.html
# rename the file
mv /usr/share/nginx/html/assets/env.js /usr/share/nginx/html/assets/env.$(sha1sum /usr/share/nginx/html/assets/env.js | cut -d' ' -f1).js


# start the web server
exec nginx -g 'daemon off;'