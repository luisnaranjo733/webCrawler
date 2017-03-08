// models

// worker states
const WORKER_IDLE = 'idle';
const WORKER_LOADING = 'loading';
const WORKER_CRAWLING = 'crawling';

class Worker {
    constructor(workerID, status) {
        this.workerID = workerID;
        this.status = status;
    }
}

// view logic (jquery dom manipulation)
function renderStats(cpuUtilization, ramAvailable, nUrlsCrawled, sizeOfQueue, sizeOfTable, sizeOfTrie, lastTrieTitle) {
    $('#cpu').text(cpuUtilization);
    $('#ramMB').text(ramAvailable);
    $('#nUrlsCrawled').text(nUrlsCrawled);
    $('#sizeOfQueue').text(sizeOfQueue);
    $('#sizeOfTable').text(sizeOfTable);
    $('#sizeOfTrie').text(sizeOfTrie);
    $('#lastTrieTitle').text(lastTrieTitle);
}

function clearStats() {
    renderStats('?', '?', '?', '?', '?');
}

function renderWorkerTable(workers) {
    for(let worker of workers) {
        let className = '';

        if (worker.status === WORKER_IDLE) {
            className = 'warning';
        } else if (worker.status === WORKER_LOADING) {
            className = 'info';
        } else if (worker.status === WORKER_CRAWLING) {
            className = 'success';
        }

        let tableRow = $('<tr/>');
        tableRow.addClass(className);
        
        let tableHead = $('<th/>');
        tableHead.attr('scope', 'row');
        tableHead.text(worker.workerID);

        let td = $('<td/>');
        td.text(worker.status);

        tableRow.append(tableHead);
        tableRow.append(td);

        $('tbody').append(tableRow);
    }
}

function clearWorkerTable() {
    $('tbody').empty();
}

function renderErrorLog(errorLog) {
    for(let error of errorLog) {
        let li = $('<li/>');
        li.addClass("list-group-item");
        li.text(error);

        $('#errorLogList').append(li);
    }
}

function clearErrorLog() {
    $('#errorLogList').empty();
}

function renderRecentlyCrawled(recentlyCrawled) {
    for(let url of recentlyCrawled) {
        let li = $('<li/>');
        li.addClass("list-group-item");
        li.text(url);

        $('#crawledList').append(li);
    }
}

function clearRecentlyCrawled() {
    $('#crawledList').empty();
}



function renderModal(title, body) {
    $('#modalTitle').text(title);
    $('#modalBody').text(body);
}

// controller logic

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

    if (!successCallback) {
        successCallback = () => { };
    }

    if (!failureCallback) {
        failureCallback = () => { };
    }

    $.ajax({
        type: requestType,
        //url: `Dashboard.asmx/${webMethodName}`,
        url: webMethod,
        data: formattedData,
        contentType: "application/json; charset=utf-8",
        dataType: "json"
    }).done((data) => {
        if (data) { 
            successCallback(JSON.parse(data.d));
        } else {
            successCallback();
        }
    }).fail(failureCallback);
}

function retrieveStats() {
    request('POST', 'Dashboard.asmx/RetrieveStats', null, stats => {
        renderStats(...stats);
    });
}

function updateWorkerStatus(refreshServerSide) {
    request('POST', 'Dashboard.asmx/RetrieveWorkerStatus', null, worker_objects => {
        let workers = worker_objects.map(worker_object => new Worker(worker_object.id, worker_object.state));

        if (refreshServerSide) { // update stats server side on the first call if worker is idle
            if (workers.length > 0) {
                let worker = workers[0];
                request('POST', 'Dashboard.asmx/UpdateStats', null, retrieveStats);
            }
        }

        clearWorkerTable();
        renderWorkerTable(workers);
    });
}


function workerStatsLoop(firstCall) {
    setTimeout(() => {
        if (firstCall) {
            updateWorkerStatus(true);
        } else {
            updateWorkerStatus(false);
        }

        workerStatsLoop(false);
    }, 2000);
}

function generalStatsLoop() {
    setTimeout(() => {
        console.log('stats loop');
        retrieveStats();
        generalStatsLoop();
    }, 2000);
}

$(document).ready(function () {
    workerStatsLoop(true);
    generalStatsLoop();

    $('#queueBleacher').click(click => {
        let url = "http://bleacherreport.com/robots.txt";
        request('POST', 'Dashboard.asmx/queueSitemap', { 'robotsURL': url }, retrieveStats);

    });

    $('#queueCnn').click(click => {
        let url = "http://www.cnn.com/robots.txt";
        request('POST', 'Dashboard.asmx/queueSitemap', { 'robotsURL': url }, retrieveStats);
    });

    $('#startLoading').click(click => {
        request('POST', 'Dashboard.asmx/StartLoading');
    });

    $('#startCrawling').click(click => {
        request('POST', 'Dashboard.asmx/StartCrawling');
    });

    $('#startIdling').click(click => {
        request('POST', 'Dashboard.asmx/StartIdling');
    });


    request('POST', 'Dashboard.asmx/GetErrorLog', null, (errorLog) => {
        renderErrorLog(errorLog);
    });

    request('POST', 'Dashboard.asmx/GetRecentUrlsCrawled', { 'n': 10 }, (crawledList) => {
        renderRecentlyCrawled(crawledList);
    });

    $('#deleteEverything').click(() => {
        request('POST', 'Dashboard.asmx/DeleteEverything');
    });

    $('#clearUrlQueue').click(() => {
        request('POST', 'Dashboard.asmx/ClearUrlQueue', null, retrieveStats);
    });


    $('#refreshStats').click(() => {
        retrieveStats();
    });

    request('POST', 'SuggestionService.asmx/IsWikiDownloaded', null, (isWikiDownloaded) => {
        if (isWikiDownloaded) { $('#downloadWiki').addClass('hidden'); }
        request('POST', 'SuggestionService.asmx/IsTrieBuilt', null, (isTrieBuilt) => {
            if (isTrieBuilt) { $('#buildTrie').addClass('hidden'); }
            if (isWikiDownloaded && isTrieBuilt) { $('#trieTitle').addClass('hidden'); }
        });
    });
});
