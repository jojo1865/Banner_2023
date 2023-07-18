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
/*取得郵遞區號*/
function GetZipCode(ddl) {
    console.log(ddl.value);
    $('#lab_ZipCode').html('');
    $.ajax({
        url: '/Admin/Home/GetZipCode?ZID=' + ddl.value,
        method: 'GET',
        dataType: 'text',
        success: function (res) {
            console.log(res);
            $('#lab_ZipCode').html(res);
        },
        error: function (err) { console.log(err) },
    })
}
/*顯示地址的不同項目區塊 */
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
/*確定送出修改 */
function SubmitConfirm() {
    Swal.fire({
        icon: 'warning',
        html: '確定儲存修改?',
        showDenyButton: true,
        confirmButtonText: '確認',
        denyButtonText: '取消'
    }).then((result) => {
        if (result.isConfirmed) {
            document.getElementById('form1').submit();
        }
        else {
            Swal.close();
            ClearLoading();
            return;
        }
    });
}
function ClearLoading() {
    btns.forEach((btn) => {
        btn.disabled = false;
        btn.classList.remove("btn-loading");
    });
}
function ShowUserMenu() {
    document.getElementById('UserMenu').style.display = "";
}

function CloseUserMenu() {
    document.getElementById('UserMenu').style.display = "none";
}
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
/*取消密碼顯示*/
function togglePasswordVisibility(visibility, controlid) {
    const passwordInput = document.getElementById(controlid);
    if (passwordInput.type === "password") {
        passwordInput.type = "text";
        visibility.textContent = "visibility";
    } else {
        passwordInput.type = "password";
        visibility.textContent = "visibility_off";
    }
}