﻿/******************************************************
 * 
 * Copyright (c) 2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

function setImg(src, idImg, idDivViewImg, idDismiss) {
    var img = document.getElementById(idImg);
    img.onload = function () {
        viewImg(this, idDivViewImg);
    }
    img.src = src;

    var dismiss = document.getElementById(idDismiss);
    dismiss.onclick = function () {
        dismissImg(idDivViewImg);
    }
}

function viewImg(img, idDivViewImg) {
    var maxFactor = 0.95;
    var xRatio = img.naturalWidth / window.innerWidth;
    var yRatio = img.naturalHeight / window.innerHeight;
    var maxRatio = (xRatio > yRatio) ? xRatio : yRatio;
    if (maxRatio > maxFactor) {
        img.width = maxFactor * (img.naturalWidth / maxRatio);
        img.height = maxFactor * (img.naturalHeight / maxRatio);
    }
    else {
        img.width = img.naturalWidth;
        img.height = img.naturalHeight;
    }
    var div = $("#" + idDivViewImg);
    div.dialog({ autoOpen: false, closeOnEscape: true, height: img.height, width: img.width, modal: true, resizable: false, draggable: false, title: null });
    div.dialog("open");
    $(".ui-dialog-titlebar").hide();    // hide the title bar
    $(".ui-dialog .ui-dialog-content").css("padding", "0");
    $(".ui-dialog").css("padding", "0");
    $(".ui-widget.ui-widget-content").css("border", "none");
    $(".ui-widget.ui-widget-content").css("border-radius", "0");
    $(".modalpopup").css("border-radius", "0");
    div.width(img.width);
    div.height(img.height);
}

function dismissImg(idDivViewImg) {
    $("#" + idDivViewImg).dialog("close");
}

function dismissDlg(idDlg) {
    $(idDlg).dialog("close");
}

function showModalById(id, szTitle, width) {
    // the dialog is placed outside of the main form, so asp.net postbacks get lost.  Thus we append these to the parent form.
    $("#" + id).dialog({ autoOpen: true, closeOnEscape: true, width: (width || 400), modal: true, title: szTitle || "" }).parent().appendTo(jQuery("form:first"));
}

function convertFdUpJsonDate(fdUpDate) {
    return new Date(parseInt(fdUpDate.replace("/Date(", "").replace(")/", "")));
}

function sortTable(sender, colIndex, sortType, hdnSortIndexID, hdnSortDirID) {
    var table = $(sender).parents('table');
    var lastSortIndex = parseInt($("#" + hdnSortIndexID).val());
    var lastSortDir = $("#" + hdnSortDirID).val();

    var order = "asc";

    if (lastSortIndex == colIndex && lastSortDir == "asc") {
        order = "desc";
    }

    $("#" + hdnSortIndexID).val(colIndex);
    $("#" + hdnSortDirID).val(order);

    table.find("th").each(function () {
        $(this).removeClass("headerSortAsc").removeClass("headerSortDesc")
    })
    $(sender).addClass(order == "asc" ? "headerSortAsc" : "headerSortDesc");

    var sortDir = (order === 'asc') ? 1 : -1;
    var selector = 'td:nth-child(' + (colIndex + 1) + ')';

    var tbody = table.children('tbody');
    tbody.children('tr').sort(function (a, b) {
        var vala = $(a).find(selector);
        var valb = $(b).find(selector);

        if (sortType == "num") {
            var aNormal = vala.find("span:hidden").first();
            var bNormal = valb.find("span:hidden").first();
            var aint = parseFloat((aNormal.length > 0 ? aNormal : vala).text());
            var bint = parseFloat((bNormal.length > 0 ? bNormal : valb).text());
            return sortDir * ((aint < bint) ? -1 : ((aint == bint) ? 0 : 1));
        } else if (sortType == "date") {
            var sortKeyA = vala.find("span:hidden").first().text();
            var sortKeyB = valb.find("span:hidden").first().text();
            return sortDir * (sortKeyA.localeCompare(sortKeyB));
        } else {
            return sortDir * (vala.text().localeCompare(valb.text()));
        }
    }).appendTo(tbody);
}

/* Image Editing Helpers */
function editImageComment(sender) {
    var parent = $(sender).parents("div[name='editImage']")
    parent.find("[name='statComment']").toggle();
    var container = parent.find("[name='dynComment']");
    container.toggle();
    if (container.is(":visible"))
        container.find("[name='txtImgComment']")[0].focus();
}

function deleteImage(confirmText, imageclass, key, thumbfile, asAdmin, onComplete) {
    if (confirmText == '' || confirm(confirmText)) {
        var params = new Object();
        params.ic = imageclass;
        params.key = key;
        params.szThumb = thumbfile;
        params.fAsAdmin = asAdmin;
        var d = JSON.stringify(params);
        $.ajax({
            url: '/logbook/mvc/Image/DeleteImage',
            type: "POST", data: d, dataType: "text", contentType: "application/json",
            error: function (xhr, status, error) { window.alert(xhr.responseText); },
            complete: function (response) { },
            success: function (response) {
                if (onComplete)
                    onComplete(response);
            }
        });
    }
}

function updateComment(imageclass, key, thumbfile, newComment, asAdmin, onComplete) {
    var params = new Object();
    params.ic = imageclass;
    params.key = key;
    params.szThumb = thumbfile;
    params.newAnnotation = newComment;
    params.fAsAdmin = asAdmin;
    var d = JSON.stringify(params);
    $.ajax({
        url: '/logbook/mvc/Image/AnnotateImage',
        type: "POST", data: d, dataType: "text", contentType: "application/json",
        error: function (xhr, status, error) { window.alert(xhr.responseText); },
        complete: function (response) { },
        success: function (response) {
            if (onComplete)
                onComplete(response);
        }
    });
}

function defaultButtonForDiv(idDiv, idButton) {
    $('#' + idDiv).keydown(function (e) {
        if (e.keyCode == 13) {
            $('#' + idButton)[0].click();
            return false;
        }
    });
}
