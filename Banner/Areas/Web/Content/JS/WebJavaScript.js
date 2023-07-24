/*取得鄉鎮市區*/
function GetZipddl(ddl) {
    console.log(ddl.value);
    $('#ddl_Zip2').html('');
    $.ajax({
        url: '/Web/Home/GetZipSelect?ZID=' + ddl.value,
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
        url: '/Web/Home/GetZipCode?ZID=' + ddl.value,
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
const btns = document.querySelectorAll(".btn_submit");
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
function LogoutCheck() {
    Swal.fire({
        icon: 'warning',
        html: '確定登出?',
        showDenyButton: true,
        confirmButtonText: '確認',
        denyButtonText: '取消'
    }).then((result) => {
        if (result.isConfirmed) {
            document.location.href = '/Web/Home/Logout';
        }
        else {
            Swal.close();
            return;
        }
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
        url: '/Web/Home/GetOISelect?OIID=' + ddl.value,
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
//更改密碼
function SavePW() {
    var Login = document.getElementById('txb_Login');
    var ACID = document.getElementById('hid_ACID');
    var Password = document.getElementById('txb_Password');
    if (Login.value == '') {
        Swal.fire({
            icon: 'error',
            html: '請輸入帳號'
        });
    }
    else if (Password.value == '') {
        Swal.fire({
            icon: 'error',
            html: '請輸入新密碼'
        });
    }
    else {
        $.ajax({
            url: '/Web/AccountPlace/ChangePW?ACID=' + ACID.value + '&Login=' + Login.value + '&PW=' + Password.value,
            method: 'PUT',
            dataType: 'text',
            success: function (res) {
                if (res == 'OK') {
                    Swal.fire({
                        icon: 'success',
                        html: '密碼已變更完成'
                    });
                    Login.value = '';
                    Password.value = '';
                }
                else {
                    Swal.fire({
                        icon: 'error',
                        html: res
                    });
                }
            },
            error: function (err) { console.log(err) },
        })
    }
    return false;
}


function SendSNSCheckPhone(ddl_Zip, txb_Phone, lab_HidPhoneNo, txb_InputCheckCode_Phone, cbox_PhoneCheck, div_1, div_2, div_3) {
    var CheckCode = '';
    //先送簡訊
    $.ajax({
        url: '/Web/Home/SendSSN?ZID=' + ddl_Zip.value + '&PhoneNo=' + txb_Phone.value,
        method: 'GET',
        dataType: 'text',
        success: function (res) {
            const Return = res.split(';');
            const labs = document.getElementsByName(lab_HidPhoneNo);
            for (i = 0; i < labs.length; i++) {
                labs[i].innerHTML = Return[0];
            }
            CheckCode = Return[1];
        }
    })
    //顯示視窗
    var div = document.getElementById('div_PhoneCheck');
    if (div != null) {
        Swal.fire({
            html: div.innerHTML,
            showCancelButton: true,
            showConfirmButton: true,
            
            cancelButtonText: '取消',
            confirmButtonText: '確定',
            customClass: {
                cancelButton: 'btn btn-outline-primary btn-lg btn-round alertbtn',
                confirmButton:'btn btn-primary btn-lg btn-round alertbtn'
            }
            
        }).then((result) => {//檢查輸入碼
            if (result.isConfirmed) {
                var txbs = document.getElementsByName(txb_InputCheckCode_Phone);
                for (i = 0; i < txbs.length; i++) {
                    if (txbs[i].value != '') {
                        document.getElementById(div_1).style.display = 'none';
                        
                        if (txbs[i].value == CheckCode || true) {
                            ddl_Zip.disabled = true;
                            txb_Phone.disabled = true;

                            document.getElementById(cbox_PhoneCheck).checked = true;
                            document.getElementById(div_2).style.display = 'block';
                            document.getElementById(div_3).style.display = 'none';
                        }
                        else {
                            document.getElementById(div_2).style.display = 'none';
                            document.getElementById(div_3).style.display = 'block';
                        }
                    }
                }
            }
        });
    }
    return false;
}