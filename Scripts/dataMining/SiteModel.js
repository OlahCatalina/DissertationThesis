/*
 *
 */

class SiteModel {

    constructor(index, name, url, categories) {
        this.index = index,
        this.name = name;
        this.url = url;
        const categoriesList = new Array(categories.split(","));
        this.categories = categoriesList;

    }

}