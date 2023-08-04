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
/*用關鍵字搜尋小組 */
function GetOIList(ACID,txb) {
    $.ajax({
        url: '/Admin/Home/GetOIList?ACID='+ACID+'&Key=' + txb.value,
        method: 'GET',
        dataType: 'json',
        success: function (res) {
            $('#ddl_OIs').html('');
            console.log(res);
            $.each(res, function (index) {
                $('#ddl_OIs').append('<option title="' + index + '" value="' + res[index].value + '">' + res[index].value + '</option>');
            });
            return;
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

const btns = document.querySelectorAll(".btn-submit");
btns.forEach((btn) => {
    btn.addEventListener("click", () => {
        btn.disabled = true;
        btn.classList.add("btn-loading");
        console.log('Add loading CSS');
    });
});
function ClearLoading() {
    btns.forEach((btn) => {
        btn.disabled = false;
        btn.classList.remove("btn-loading");
        console.log('Remove loading CSS');
    });
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
/*聚會點地圖高度校正 */
window.onload = function () {
    var divs = $('.div_OI');
    Array.prototype.forEach.call(divs, function (div) {
        div.style.height = div.clientHeight + 'px';
    });
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
function ShowUserMenu() {
    document.getElementById('BackUserMenu').style.display = "";
}

function CloseUserMenu() {
    document.getElementById('BackUserMenu').style.display = "none";
}

function LogoutCheck() {
    Swal.fire({
        icon: 'warning',
        html: '確定登出?',
        showDenyButton: true,
        confirmButtonText: '確認',
        denyButtonText: '取消'
    }).then((result) => {
        if (result.isConfirmed) {
            document.location.href = '/Admin/Home/Logout';
        }
        else {
            Swal.close();
            return;
        }
    });
}
//換密碼檢查
function CheckInput(ACID) {

    var sOld = document.getElementById('txb_Old').value;
    var sNew1 = document.getElementById('txb_New1').value;
    var sNew2 = document.getElementById('txb_New2').value;
    $.ajax({
        url: '/Admin/Home/CheckPasswordInput?ACID=' +ACID+'&Old=' + sOld + '&New1=' + sNew1 + '&New2=' + sNew2,
        method: 'GET',
        dataType: 'text',
        success: function (res) {
            //console.log(res);
            if (res == '') {
                SubmitConfirm();
            } else {
                Swal.fire({
                    icon: 'error',
                    html: res
                });
                ClearLoading();
            }
        },
        error: function (err) { console.log(err) },
    })
}
//動態顯示資訊
function SetAlertNote(Msg) {
    Swal.fire({
        icon: 'Info',
        html: Msg
    });
    ClearLoading();
}
//資料啟用/停用
function ChangeActive(TargetControl,TableName, ID) {
    $.ajax({
        url: '/Admin/Home/ChangeActive?TableName=' + TableName + '&ID=' + ID,
        method: 'GET',
        dataType: 'text',
        success: function (res) {
            if (TargetControl.classList.contains('btn-outline-success')) {
                TargetControl.classList.replace('btn-outline-success', 'btn-outline-danger');
                TargetControl.innerHTML = '停用';
            }
            else {
                TargetControl.classList.replace('btn-outline-danger', 'btn-outline-success');
                TargetControl.innerHTML = '啟用';
            }
        },
        error: function (err) { console.log(err) },
    })
    
}
//如期受洗
function Baptized(ACID) {
    $.ajax({
        url: '/Admin/AccountSet/Account_Baptized_SetDate?ACID=' + ACID,
        method: 'GET',
        dataType: 'text'
    })
    Swal.fire({
        icon: 'success',
        html: '存檔完成'
    });
    location.href = '/Admin/AccountSet/Account_Baptized_List';
    return;
}


/*檢查輸入 */
// Example starter JavaScript for disabling form submissions if there are invalid fields
(() => {
    'use strict'

    // Fetch all the forms we want to apply custom Bootstrap validation styles to
    const forms = document.querySelectorAll('.needs-validation')

    // Loop over them and prevent submission
    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault()
                event.stopPropagation()
            }

            form.classList.add('was-validated')
        }, false)
    })
})()
/*切換顯示受洗選單 */
function ShowBaptizedTypeddl(rbut) {
    document.getElementById('ddl_BaptizedType').style.display = (rbut.checked ? "" : "none");
}
function HideBaptizedTypeddl(rbut) {
    document.getElementById('ddl_BaptizedType').style.display = (!rbut.checked ? "" : "none");
}