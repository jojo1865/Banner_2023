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
    if (txb_Phone.value != '') {
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
        });
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
                    confirmButton: 'btn btn-primary btn-lg btn-round alertbtn'
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
    }
    else {
        alert('請輸入手機號碼');
    }
    return ;
}
function SendSNSCheckEmail(txb_Email, lab_HidEmail, txb_InputCheckCode_Email, cbox_EmailCheck, div_1, div_2, div_3) {
    var CheckCode = '';
    if (txb_Email.value != '') {
        //先送Email
        $.ajax({
            url: '/Web/Home/SendMailCode?email=' + txb_Email.value,
            method: 'GET',
            dataType: 'text',
            success: function (res) {
                const Return = res.split(';');
                const labs = document.getElementsByName(lab_HidEmail);
                for (i = 0; i < labs.length; i++) {
                    labs[i].innerHTML = Return[0];
                }
                CheckCode = Return[1];
            }
        });
        //顯示視窗
        var div = document.getElementById('div_EmailCheck');
        if (div != null) {
            Swal.fire({
                html: div.innerHTML,
                showCancelButton: true,
                showConfirmButton: true,

                cancelButtonText: '取消',
                confirmButtonText: '確定',
                customClass: {
                    cancelButton: 'btn btn-outline-primary btn-lg btn-round alertbtn',
                    confirmButton: 'btn btn-primary btn-lg btn-round alertbtn'
                }

            }).then((result) => {//檢查輸入碼
                if (result.isConfirmed) {
                    var txbs = document.getElementsByName(txb_InputCheckCode_Email);
                    for (i = 0; i < txbs.length; i++) {
                        if (txbs[i].value != '') {
                            document.getElementById(div_1).style.display = 'none';

                            if (txbs[i].value == CheckCode || true) {
                                ddl_Zip.disabled = true;
                                txb_Phone.disabled = true;

                                document.getElementById(cbox_EmailCheck).checked = true;
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
    }
    else {
        alert('請輸入Email');
    }
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
/*檢查Email*/
function checkemail(txb_email) {
    var email = txb_email.value;
    if (email == '') {
        Swal.fire({
            icon: 'error',
            html: '請輸入電子信箱'
        });
        txb_email.focus();
        return false;
    } else {
        var emailRegxp = /^([\w]+)(.[\w]+)*@([\w]+)(.[\w]{2,3}){1,2}$/;
        if (emailRegxp.test(email) != true) {
            Swal.fire({
                icon: 'error',
                html: '電子信箱格式錯誤'
            });
            txb_email.focus();
            txb_email.select();
            return false;
        }
    }
}
function checkLogin(txb) {
    if (txb.value != "") {
        fetch('/Web/AccountAdd/CheckLogin?input=' + txb.value)
            .then(function (response) {
                return response.json();
            })
            .then(function (myJson) {
                console.log(myJson.res);
                if (myJson.res == 'OK') {
                    document.getElementById('cbox_Login_Success').checked = true;
                    document.getElementById('div_Login').innerHTML = '';
                }
                else {
                    document.getElementById('cbox_Login_Success').checked = false;
                    document.getElementById('div_Login').innerHTML = '<span style="color:red">' + myJson.res + '</span>';
                }
            });

        
        /*$.ajax({
            url: '/Web/AccountAdd/CheckLogin?input=' + txb.value,
            method: 'GET',
            dataType: 'text',
            success: function (res) {
                
            }
        });*/

        /*document.getElementById('cbox_Login_Success').checked = true;
        document.getElementById('div_Login').innerHTML = '';
        */
    }
    else {
        document.getElementById('cbox_Login_Success').checked = false;
        document.getElementById('div_Login').innerHTML = '';
    }
    return;
}
//檢查密碼
function CheckPassword() {
    var PW1 = document.getElementById("txb_PW1");
    var PW2 = document.getElementById("txb_PW2");
    var div_PW1 = document.getElementById("div_PW1");
    var div_PW2 = document.getElementById("div_PW2");
    div_PW1.innerHTML = '';
    div_PW2.innerHTML = '';
    var bSuccess = true;
    var cboxSuccess = document.getElementById('cbox_PW_Success');
    if (PW1.value != "" && PW2.value != "") {
        if (PW1.value != PW2.value) {
            div_PW2.innerHTML = '<span style="color:red">兩次密碼輸入不同</span>';
            bSuccess = false;
        }
        else if (PW1.value.length < 8) {
            div_PW1.innerHTML = '<span style="color:red">密碼長度不足,請在8個字元以上</span>';
            bSuccess = false;
        }
        else if (PW1.value.length > 12) {
            div_PW1.innerHTML = '<span style="color:red">密碼長度過長,請在12個字元以內</span>';
            bSuccess = false;
        }
        else {
            const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/;
            var isStrongPassword = regex.test(PW1.value);
            if (!isStrongPassword) {
                div_PW1.innerHTML = '<span style="color:red">密碼強度不足</span>';
                bSuccess = false;
            }
        }

        if (bSuccess) {
            cboxSuccess.checked = true;
        }
        else {
            cboxSuccess.checked = false;
        }
    }
    else {
        cboxSuccess.checked = false;
    }
    
    return;
}

/*發送驗證碼+倒數計時 */
function SendCodeAtStep2() {
    document.getElementById('a_Send').style.display = 'none';
    document.getElementById('lab_msg').style.display = 'flex';
    var rbut_Con1 = document.getElementById('rbut_Con1');
    var rbut_Con2 = document.getElementById('rbut_Con2');
    var CheckCode = '';
    if (rbut_Con1.checked) {
        var txb_PhoneZip = document.getElementById('txb_PhoneZip');
        var txb_Phone = document.getElementById('txb_Phone');
        $.ajax({
            url: '/Web/Home/SendSSN?ZID=' + txb_PhoneZip.value + '&PhoneNo=' + txb_Phone.value,
            method: 'GET',
            dataType: 'text',
            success: function (res) {
                const Return = res.split(';');
                CheckCode = Return[1];
                document.getElementById('txb_Code_Get').value = CheckCode;
            }
        });
    }
    else {
        var txb_Email = document.getElementById('txb_Email');
        $.ajax({
            url: '/Web/Home/SendMailCode?email=' + txb_Email.value,
            method: 'GET',
            dataType: 'text',
            success: function (res) {
                const Return = res.split(';');
                CheckCode = Return[1];
                document.getElementById('txb_Code_Get').value = CheckCode;
            }
        });
    }
    timer(59);
}
const countdownClockTitle = document.getElementById("lab_wait");
let countdown;

const showTimer = (inputSeconds) => {
    const minutes = Math.floor(inputSeconds / 60);
    const remainderSeconds = inputSeconds % 60;
    countdownClockTitle.textContent = `
        ${remainderSeconds < 10 ? "0" + remainderSeconds : remainderSeconds}
    `;
}

function timer(inputSeconds) {
    const now = Date.now();
    const done = now + inputSeconds * 1000;
    showTimer(inputSeconds);
    countdown = setInterval(() => {
        const tiemleft = Math.round((done - Date.now()) / 1000);
        if (tiemleft <= 0) {
            clearInterval(countdown);
            document.getElementById('lab_msg').style.display = 'none';
            document.getElementById('a_Send').style.display = 'flex';



            return  // 結束執行setInterval，不再執行下面程式碼
        }
        showTimer(tiemleft)
    }, 1000)
}
/*顯示"線上奉獻規範與個資使用說明" */
function ShowNote() {
    var Note = document.getElementById('div_Note');
    Swal.fire({
        showCloseButton: true,
        showConfirmButton: false,
        title:'線上奉獻規範與個資使用說明',
        html: Note.innerHTML
    });
    return;
}
/*切換顯示受洗選單 */
function ShowBaptizedTypeddl(rbut) {
    document.getElementById('ddl_BaptizedType').style.display = (rbut.checked ? "" : "none");
}
function HideBaptizedTypeddl(rbut) {
    document.getElementById('ddl_BaptizedType').style.display = (!rbut.checked ? "" : "none");
}