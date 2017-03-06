/* DOM manipulation */

// show the div that contains search suggestions
function showSearchSuggestions() {
    $('#searchSuggestions').css('display', 'block');
}

// hide the div that contains search suggestions
function hideSearchSuggestions() {
    $('#searchSuggestions').css('display', 'none');
    if ($('#searchQuery').val() === '') {
        $('#searchSuggestions').css('display', 'none');
    }
}

// render an array of search result strings (replace state)
function renderSearchSuggestions(searchSuggestions) {
    $('ul').empty();
    for (let searchSuggestion of searchSuggestions) {
        $('#searchSuggestions ul').append(`<li>${searchSuggestion}</li>`);
    }
}

let showRankedResults = false;

function renderSearchResults(searchResults) {
    $('#searchResults').empty();
    for (let searchResult of searchResults) {
        $('#searchResults').append(searchResult.toDom());

    }
}

/* model */

class ArticleResult {
    constructor(title, url, date) {
        this.title = title;
        this.url = url;
        this.date = date;
    }

    toDom() {
        let a = $('<a/>');
        a.attr('href', this.url);
        a.addClass('list-group-item');

        let h4 = $('<h4/>');
        h4.addClass('list-group-item-heading');
        h4.text(this.title);

        let url = $('<p/>');
        url.addClass('list-group-item-text');
        url.text(this.url);

        a.append(h4);
        a.append(url);  

        if (this.date) {
            let date = $('<p/>');
            date.addClass('list-group-item-text');
            date.text(this.date);
            a.append(date);
        }

        return a;
    }
}

class NbaResult {
    constructor(freeThrowPct, gamesPlayed, name, pointsPerGame, team, threePointPct) {
        this.freeThrowPct = freeThrowPct;
        this.gamesPlayed = gamesPlayed;
        this.name = name;
        this.pointsPerGame = pointsPerGame;
        this.team = team;
        this.threePointPct = threePointPct;
    }

    buildP(text) {
        let p = $('<p/>');
        p.addClass('list-group-item-text');
        p.text(text);
        return p;
    }

    toDom() {
        let a = $('<a/>');
        a.addClass('list-group-item');

        let h4 = $('<h4/>');
        h4.addClass('list-group-item-heading');
        h4.text(this.name);

        a.append(h4);
        a.append(this.buildP(`Team: ${this.team}`));
        a.append(this.buildP(`Free throw pct: ${this.freeThrowPct}`));
        a.append(this.buildP(`Games played: ${this.gamesPlayed}`));
        a.append(this.buildP(`Points per game: ${this.pointsPerGame}`));
        a.append(this.buildP(`Three point pct: ${this.threePointPct}`));
        return a;
    }
}


/* Logic */

function request(requestType, webMethod, params, successCallback, failureCallback) {
    let formattedData = '';
    if (params) {
        formattedData = '{';
        for (let key of Object.keys(params)) {
            let value = params[key];
            formattedData = formattedData.concat(`"${key}": "${value}",`);
        }
        formattedData = formattedData.slice(0, formattedData.length - 1); // remove last trailing comma
        formattedData = formattedData.concat('}');
    }

    if (successCallback === null) {
        successCallback = () => { };
    }

    if (failureCallback === null) {
        failureCallback = () => { };
    }

    $.ajax({
        type: requestType,
        url: webMethod,
        data: formattedData,
        contentType: "application/json; charset=utf-8",
        dataType: "json"
    }).done(data => successCallback(JSON.parse(data.d))
    ).fail(failureCallback);
}


// callback when the search query value changes
$('#searchQuery').on('change keyup', (event) => {
    let searchQuery = $('#searchQuery').val();
    if (searchQuery) {
        request('POST', 'SuggestionService.asmx/searchTrie', { 'query': searchQuery }, renderSearchSuggestions);

        if ($('#searchSuggestions').css('display') === 'none' && !showRankedResults) {
            // this is needed to handle the case where they start typing without losing focus and the suggestions don't show up
            showSearchSuggestions();
        }
        showRankedResults = false;
    } else {
        hideSearchSuggestions();
    }
});

$(document).keypress(function (e) {
    if (e.which === 13) { // pressed enter
        e.preventDefault();
        showRankedResults = true;
        hideSearchSuggestions();

        let searchQuery = $('#searchQuery').val();

        let PA1Results = [];

        $.ajax({ // jsonp request
            crossDomain: true,
            contentType: "application/json; charset=utf-8",
            url: "http://ec2-54-244-57-120.us-west-2.compute.amazonaws.com/info344/hwk1/api.php",
            data: {
                "searchQuery": searchQuery
            }, 
            dataType: "jsonp"
        }).then(function (data) {
            PA1Results = data.map((result) => {
                return new NbaResult(result.freeThrowPct, result.gamesPlayed, result.name, result.pointsPerGame, result.team, result.threePointPct);
            });
            
            request('POST', 'Dashboard.asmx/RetrieveSearchResults', { 'searchQuery': searchQuery },
                (PA2Results) => {
                    PA2Results = PA2Results.map((result) => {
                        let date;
                        if (result.Date) {
                            date = new Date(parseInt(result.Date.substr(6)));
                        }
                        
                        return new ArticleResult(result.Title, result.Url, date);
                    });
                    
                    let searchResults = PA1Results.concat(PA2Results);
                    renderSearchResults(searchResults);
                }, (response) => {
                    if (response.status == "500") {
                        console.log(response);
                        alert("Server error");
                    }
                });
        });
    }
    
});



$('#searchQuery').focus((event) => {
    showSearchSuggestions();
});

$('#searchQuery').focusout((event) => {
    hideSearchSuggestions();
});