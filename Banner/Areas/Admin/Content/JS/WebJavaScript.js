/*產生組織聚會點圖樣*/
$(function () {
    var treedata = JSON.parse(document.getElementById('hid_treedata').value);
    $('#chart-container').orgchart({
        'data': treedata,
        'nodeContent': 'title',
        'direction': 'l2r'
    });
});
/*取得鄉鎮市區*/
function GetZipddl(ddl) {
    console.log(ddl.value);
    $('#ddl_Zip2').html('');
    $.ajax({
        url: '/Admin/Home/GetZipSelect?ZID=' + ddl.value,
        method: 'GET',
        dataType: 'json',
        success: function (res) {
            console.log(res);
            $.each(res, function (index) {
                $('#ddl_Zip2').append('<option value="' + res[index].value + '">' + res[index].Text + '</option>');
            });
        },
        error: function (err) { console.log(err) },
    })
}
function ShowArea(ddl) {
    var Z0s = document.getElementsByClassName("div_Zip0");
    var Z1s = document.getElementsByClassName("div_Zip1");
    var Z2s = document.getElementsByClassName("div_Zip2");
    if (ddl.value == 10)//本國
    {
        Array.prototype.forEach.call(Z0s, function (Z) {
            Z.style.display = '';
        });
        Array.prototype.forEach.call(Z1s, function (Z) {
            Z.style.display = 'none';
        });
        Array.prototype.forEach.call(Z2s, function (Z) {
            Z.style.display = 'none';
        });
    }
    else if (ddl.value == 2)//外國
    {
        Array.prototype.forEach.call(Z0s, function (Z) {
            Z.style.display = 'none';
        });
        Array.prototype.forEach.call(Z1s, function (Z) {
            Z.style.display = '';
        });
        Array.prototype.forEach.call(Z2s, function (Z) {
            Z.style.display = 'none';
        });
    }
    else {
        Array.prototype.forEach.call(Z0s, function (Z) {
            Z.style.display = 'none';
        });
        Array.prototype.forEach.call(Z1s, function (Z) {
            Z.style.display = 'none';
        });
        Array.prototype.forEach.call(Z2s, function (Z) {
            Z.style.display = '';
        });
    }
    return;
}
/*產生按鈕等待圖示 */
const btns = document.querySelectorAll(".btn");
btns.forEach((btn) => {
    btn.addEventListener("click", () => {
        btn.disabled = true;
        btn.classList.add("btn-loading");
    });
});

/*取得組織選單*/
function GetOIddl(ddl) {
    //console.log(ddl.value);
    var iSort = parseInt(ddl.name.replace('ddl_', ''), 10) + 1;
    for (i = iSort; i < 10; i++) {
        var Nextddl_ = $("select[name='ddl_" + i + "']");
        Nextddl_.html('');
    }

    var NextOIName = 'ddl_' + iSort;
    var Nextddl = $("select[name='" + NextOIName + "']");
    Nextddl.html('');
    Nextddl.append('<option value="-1" selected>請選擇</option>');
    $.ajax({
        url: '/Admin/Home/GetOISelect?OIID=' + ddl.value,
        method: 'GET',
        dataType: 'json',
        success: function (res) {
            //console.log(res);
            $.each(res, function (index) {
                Nextddl.append('<option value="' + res[index].value + '">' + res[index].Text + '</option>');
            });
        },
        error: function (err) { console.log(err) },
    })
}
/*聚會點地圖高度校正 */
window.onload = function () {
    var divs = $('.div_OI');
    Array.prototype.forEach.call(divs, function (div) {
        div.style.height = div.clientHeight + 'px';
    });
}