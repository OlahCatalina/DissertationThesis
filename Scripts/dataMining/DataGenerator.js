/*
 *
 */
class DataGenerator {

    constructor() {

        // Bayes Classifier
        this.classifier = new Classifier();

        // The file containing the list of sites to classify
        this.sitesFile = "files/sites.txt";

        // The file containing results after classification in form of [site|category] on each row
        this.resultFile = "files/results.txt";

        this.urlOfWebServerForChrome = "http://127.0.0.1:8887/DataMining/";
    }

    getListOfSites() {

   
        const textFromFile = this._readTextFile();
        const lines = textFromFile.split("\n");
        const listOfSites = new Array();

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].replace(/(\r\n|\n|\r)/g, "");
            const site = new SiteModel(i+1, line.split("|||")[0], line.split("|||")[1], line.split("|||")[2]);
            listOfSites.push(site);
        }
        
        return listOfSites;
    }

    readSiteText(siteUrl) {
        // As with JSON, use the Fetch API & ES6
        fetch(siteUrl)
            .then(response => response.text())
            .then(data => {
                // Do something with your data
                console.log(data);
                debugger;
            });
    }
    downloadChanges(text) {
        var textToWrite = text;
        var textFileAsBlob = new Blob([textToWrite], { type: 'text/plain' });
        var fileNameToSaveAs = "sites.txt";
        var downloadLink = document.createElement("a");
        downloadLink.download = fileNameToSaveAs;
        downloadLink.innerHTML = "Download File";
        if (window.webkitURL != null) {
            // Chrome allows the link to be clicked
            // without actually adding it to the DOM.
            downloadLink.href = window.webkitURL.createObjectURL(textFileAsBlob);
        }
        else {
            // Firefox requires the link to be added to the DOM
            // before it can be clicked.
            downloadLink.href = window.URL.createObjectURL(textFileAsBlob);
            downloadLink.onclick = destroyClickedElement;
            downloadLink.style.display = "none";
            document.body.appendChild(downloadLink);
        }

        downloadLink.click();
    }

    // Accepts a url and a callback function to run.
    requestCrossDomain(site, callback) {

        // If no url was passed, exit.
        if (!site) {
            alert('No site was passed.');
            return false;
        }

        // Take the provided url, and add it to a YQL query. Make sure you encode it!
        var yql = 'http://query.yahooapis.com/v1/public/yql?q=' + encodeURIComponent('select * from html where url="' + site + '"') + '&format=xml&callback=cbFunc';

        // Request that YSQL string, and run a callback function.
        // Pass a defined function to prevent cache-busting.
        $.getJSON(yql, cbFunc);

        function cbFunc(data) {
            // If we have something to work with...
            if (data.results[0]) {
                // Strip out all script tags, for security reasons.
                // BE VERY CAREFUL. This helps, but we should do more. 
                data = data.results[0].replace(/<script[^>]*>[\s\S]*?<\/script>/gi, '');

                // If the user passed a callback, and it
                // is a function, call it, and send through the data var.
                if (typeof callback === 'function') {
                    callback(data);
                }
            }
            // Else, Maybe we requested a site that doesn't exist, and nothing returned.
            else throw new Error('Nothing returned from getJSON.');
        }
    }

    _readTextFile() {

        $.get(
            'http://www.corsproxy.com/' +
            'en.wikipedia.org/wiki/Cross-origin_resource_sharing',
            function (response) {
                console.log("> ", response);
                $("#viewer").html(response);
            });

        let allText = "";
        const rawFile = new XMLHttpRequest();

        rawFile.open("GET", this.urlOfWebServerForChrome + this.sitesFile, false);
        rawFile.send(null);

        if (rawFile.status === 200) {
            allText = rawFile.responseText;
        }

        return allText;
    }

    

}
