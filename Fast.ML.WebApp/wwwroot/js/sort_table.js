function sortTable(n, tableId) {
    let table, rows, switching, i, x, y, shouldSwitch, order, switchCount = 0;
    table = document.getElementById(tableId);
    if (table.getElementsByTagName("th")[n].innerHTML.endsWith("▼")) {
        table.getElementsByTagName("th")[n].innerHTML = table.getElementsByTagName("th")[n].innerHTML.replace("▼", "▲");
    } else {
        table.getElementsByTagName("th")[n].innerHTML = table.getElementsByTagName("th")[n].innerHTML.replace("▲", "▼");
    }
    switching = true;
    order = "asc";
    while (switching) {
        switching = false;
        rows = table.rows;
        for (i = 1; i < (rows.length - 1); i++) {
            shouldSwitch = false;
            x = rows[i].getElementsByTagName("td")[n];
            y = rows[i + 1].getElementsByTagName("td")[n];
            if (order === "asc") {
                if (x.innerHTML.toLowerCase() > y.innerHTML.toLowerCase()) {
                    shouldSwitch = true;
                    break;
                }
            } else if (order === "desc") {
                if (x.innerHTML.toLowerCase() < y.innerHTML.toLowerCase()) {
                    shouldSwitch = true;
                    break;
                }
            }
        }
        if (shouldSwitch) {
            rows[i].parentNode.insertBefore(rows[i + 1], rows[i]);
            switching = true;
            switchCount++;
        } else {
            if (switchCount === 0 && order === "asc") {
                order = "desc";
                switching = true;
            }
        }
    }
}