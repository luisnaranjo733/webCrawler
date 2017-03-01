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
function renderStats(cpuUtilization, ramAvailable, nUrlsCrawled, sizeOfQueue, sizeOfTable) {
    $('#cpu').text(cpuUtilization);
    $('#ramMB').text(ramAvailable);
    $('#nUrlsCrawled').text(nUrlsCrawled);
    $('#sizeOfQueue').text(sizeOfQueue);
    $('#sizeOfTable').text(sizeOfTable);
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

function request(requestType, webMethodName, params, successCallback, failureCallback) {
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
        url: `Dashboard.asmx/${webMethodName}`,
        data: formattedData,
        contentType: "application/json; charset=utf-8",
        dataType: "json"
    }).done(data => successCallback(JSON.parse(data.d))
    ).fail(failureCallback);
}

function updateStats() {
    request('POST', 'RetrieveStats', null, stats => {
        renderStats(...stats);
    });
}

function updateWorkerStatus() {
    request('POST', 'RetrieveWorkerStatus', null, worker_objects => {
        let workers = worker_objects.map(worker_object => new Worker(worker_object.id, worker_object.state));
        clearWorkerTable();
        renderWorkerTable(workers);
    });
}


function workerStatsLoop() {
    setTimeout(() => {
        updateWorkerStatus();
        workerStatsLoop();
    }, 2000)
}

$(document).ready(function () {
    updateStats();
    workerStatsLoop();

    $('#queueBleacher').click(click => {
        let url = "http://bleacherreport.com/robots.txt";
        request('POST', 'queueSitemap', { 'robotsURL': url }, () => {
            //updatestats();
        });

    });

    $('#queueCnn').click(click => {
        let url = "http://www.cnn.com/robots.txt";
        request('POST', 'queueSitemap', { 'robotsURL': url }, () => {
            //updateStats();
        });
    });

    $('#startLoading').click(click => {
        request('POST', 'StartLoading', null, () => {
            //updateWorkerStatus();
            //updateStats();
        });
    });

    $('#startCrawling').click(click => {
        request('POST', 'StartCrawling', null, () => {
            //updateWorkerStatus();
            //updateStats();
        });
    });

    $('#startIdling').click(click => {
        request('POST', 'StartIdling', null, () => {
            //updateWorkerStatus();
            //updateStats();
        });
    });

    $('#retrievePageTitle').click(() => {
        let url = $('#indexedUrl').val();
        request('POST', 'RetrievePageTitle', { 'url': url }, (title) => {
            renderModal('Page title', title);
            $('#mainModal').modal('show');
        });
    });

    request('POST', 'GetErrorLog', null, (errorLog) => {
        renderErrorLog(errorLog);
    });

    request('POST', 'GetRecentUrlsCrawled', { 'n': 10 }, (crawledList) => {
        console.log('crawled list');
        console.log(crawledList);
        renderRecentlyCrawled(crawledList);
    });

    $('#deleteEverything').click(() => {
        request('POST', 'DeleteEverything');
    });

    $('#clearUrlQueue').click(() => {
        request('POST', 'ClearUrlQueue');
    });


    $('#refreshStats').click(() => {
        console.log('refresh button clicked');
        updateStats();
    });
});
