/* DOM manipulation */

// show the div that contains search suggestions
function showSearchSuggestions() {
    $('#searchSuggestions').css('display', 'block');
};

// hide the div that contains search suggestions
function hideSearchSuggestions() {
    if ($('#searchQuery').val() === '') {
        $('#searchSuggestions').css('display', 'none');
    }
};

// render an array of search result strings (replace state)
function renderSearchSuggestions(searchSuggestions) {
    $('ul').empty();
    for (let searchSuggestion of searchSuggestions) {
        $('#searchSuggestions ul').append(`<li>${searchSuggestion}</li>`);
    }
}

/* Logic */



// callback when the search query value changes
$('#searchQuery').on('change keyup', (event) => {
    let searchQuery = $('#searchQuery').val();
    if (searchQuery) {

        $.ajax({
            type: "POST",
            url: "SuggestionService.asmx/searchTrie",
            data: `{ "query": "${searchQuery}" }`,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
        }).done(
            (data) => {
                renderSearchSuggestions(JSON.parse(data.d));
            }
        ).fail((error) => {
            console.log(error);
        });

        if ($('#searchSuggestions').css('display') === 'none') {
            // this is needed to handle the case where they start typing without losing focus and the suggestions don't show up
            showSearchSuggestions();
        }
    } else {
        hideSearchSuggestions();
    }
});

$(document).keypress(function (e) {
    if (e.which == 13) {
        alert('You pressed enter!');
    }
});



$('#searchQuery').focus((event) => {
    showSearchSuggestions();
});

$('#searchQuery').focusout((event) => {
    hideSearchSuggestions();
});