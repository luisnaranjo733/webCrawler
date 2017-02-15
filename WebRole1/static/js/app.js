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

// controller logic


$(document).ready(function () {
    renderStats('47%', '31mb', '4,802', '7', '1,201 rows');

    let workers = [
        new Worker('1', WORKER_CRAWLING),
        new Worker('2', WORKER_LOADING),
        new Worker('3', WORKER_LOADING),
        new Worker('4', WORKER_IDLE),
    ];
    renderWorkerTable(workers);

    let errorLog = [
        'stuff went wrong here because X',
        'stuff went wrong over there because Y',
    ];
    renderErrorLog(errorLog);
});
