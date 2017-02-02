github URL :  https://github.com/Lucav17/hwk2Trie

URL : http://lucav17.cloudapp.net

The application is developed on the Azure platform. I had to create a cloud instance and a scheduler job instance.
The cloud instance contained all my code. I built a method that would download my txt file that i uploaded to my blob container.
Once I had connected to my blob container, I retrieved the txt file through a memory stream and then called my build trie method. I used the downloaded file and parsed it line by line to create my Trie structure. I then saved the Trie to the cache to be used to help speed up searching on my site once the user revisits the site. After this, I implemented a search method that takes in a passed value and then searches the trie for all values that start with and contain what the user is inputting by making an AJAX call to my search method on the server. This passes JSON data back to the front end html file that i use and I process the data using Javascript to build datalist options as part of the autocomplete feature. I then set up a scheduler job to ping my site every 5 minutes to keep the Trie in the cache.
