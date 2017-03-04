/* DOM manipulation */

// show the div that contains search suggestions
function showSearchSuggestions() {
    $('#searchSuggestions').css('display', 'block');
};

// hide the div that contains search suggestions
function hideSearchSuggestions() {
    $('#searchSuggestions').css('display', 'none');
    if ($('#searchQuery').val() === '') {
        $('#searchSuggestions').css('display', 'none');
    }
};

// render an array of search result strings (replace state)
function renderSearchSuggestions(searchSuggestions) {
    console.log(searchSuggestions);
    $('ul').empty();
    for (let searchSuggestion of searchSuggestions) {
        $('#searchSuggestions ul').append(`<li>${searchSuggestion}</li>`);
    }
}

let showRankedResults = false;

function renderSearchResults(searchResults) {
    $('#searchResults').empty();
    for (let searchResult of searchResults) {
        console.log(searchResult);
        let a = $('<a/>');
        a.addClass('list-group-item');

        let h4 = $('<h4/>');
        h4.addClass('list-group-item-heading');
        h4.text(searchResult.title);

        let p = $('<p/>');
        p.addClass('list-group-item-text');
        p.text(searchResult.url);

        a.append(h4);
        a.append(p);
        $('#searchResults').append(a);

//        <a class="list-group-item">
//    <h4 class="list-group-item-heading">Title</h4>
//        <p class="list-group-item-text">http://adfasdfasdf.com</p>
    //</a>

    }
}

/* model */

class SearchResult {
    constructor(title, url) {
        this.title = title;
        this.url = url;
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

function fetchPA1Results(searchQuery) {
    results = [];
    results.push(new SearchResult("lebron james", "http://www.lebron.com"));
    return results;
}

function fetchPA2Results(searchQuery) {
    results = [];
    results.push(new SearchResult("Trump blows up Zimbaba", "http://www.trump.com"));
    results.push(new SearchResult("Felix traded", "http://mariners.com"));
    return results;
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
    if (e.which == 13) { // pressed enter
        e.preventDefault();
        showRankedResults = true;
        hideSearchSuggestions();

        let searchQuery = $('#searchQuery').val();
        let PA1Results = fetchPA1Results(searchQuery);
        let PA2Results = fetchPA2Results(searchQuery);
        let searchResults = PA1Results.concat(PA2Results);
        renderSearchResults(searchResults);
    }
    
});



$('#searchQuery').focus((event) => {
    showSearchSuggestions();
});

$('#searchQuery').focusout((event) => {
    hideSearchSuggestions();
});