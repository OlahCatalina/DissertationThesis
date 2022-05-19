/*
 *
 */

class SiteTextCategory {

    constructor(url, text, categories) {
        this.url = url;
        this.text = text;
        const categoriesList = new Array(categories.split(","));
        this.categories = categoriesList;

    }

}