
/* =========================================================
   MAIN.JS - GEAR SPORT
   Tích hợp giỏ hàng với SQL Server thông qua CartController
   ========================================================= */


/* =========================================================
   1. HÀM HỖ TRỢ GIỎ HÀNG
   ========================================================= */
    
// Xác định giỏ hàng riêng của từng tài khoản
function getCartKey() {

    if (
        window.isUserLoggedIn !== true ||
        !window.currentUserId ||
        String(window.currentUserId).trim() === ""
    ) {
        return null;
    }

    if (
        String(window.currentUserRole || "").trim().toLowerCase() === "admin"
    ) {
        return null;
    }

    return "cart_" + String(window.currentUserId).trim();
}


// Đọc giỏ hàng từ localStorage
function getCart() {

    const cartKey = getCartKey();

    if (!cartKey) return [];

    try {
        const cart = JSON.parse(localStorage.getItem(cartKey)) || [];
        return Array.isArray(cart) ? cart : [];
    } catch (error) {
        console.error("Không thể đọc giỏ hàng:", error);
        return [];
    }
}


// Lưu giỏ hàng vào localStorage
function saveCart(cart) {

    const cartKey = getCartKey();

    if (!cartKey) {
        console.error("Không xác định được tài khoản hiện tại.");
        return false;
    }

    try {
        localStorage.setItem(cartKey, JSON.stringify(cart));
        return true;
    } catch (error) {
        console.error("Không thể lưu giỏ hàng:", error);
        alert("Không thể lưu giỏ hàng. Vui lòng kiểm tra trình duyệt.");
        return false;
    }
}


// Lấy tồn kho từ nút sản phẩm
function getProductStock(button) {

    const stockValue = button.getAttribute("data-stock");

    if (stockValue === null || stockValue.trim() === "") {
        return null;
    }

    const stock = Number(stockValue);

    if (!Number.isInteger(stock) || stock < 0) {
        return null;
    }

    return stock;
}


/* =========================================================
   2. KẾT NỐI CARTCONTROLLER
   ========================================================= */

// Lấy URL API
function getCartApiUrl(action) {
    return "/Cart/" + action;
}


// Gửi yêu cầu POST đến CartController
async function callCartApi(action, data = {}) {

    const formData = new URLSearchParams();

    Object.keys(data).forEach(function (key) {
        formData.append(key, data[key]);
    });

    const response = await fetch(getCartApiUrl(action), {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8"
        },
        body: formData.toString(),
        credentials: "same-origin"
    });

    if (!response.ok) {
        throw new Error("Không thể kết nối đến máy chủ.");
    }

    const result = await response.json();

    if (!result.success) {
        throw new Error(result.message || "Thao tác thất bại.");
    }

    return result;
}


// Lấy giỏ hàng mới nhất từ SQL Server
async function syncCartFromServer() {

    try {

        const response = await fetch(getCartApiUrl("GetCart"), {
            method: "GET",
            credentials: "same-origin"
        });

        if (!response.ok) {
            throw new Error("Không thể tải giỏ hàng.");
        }

        const result = await response.json();

        if (!result.success) {
            throw new Error(result.message || "Không thể đồng bộ giỏ hàng.");
        }

        const items = result.items || [];

        saveCart(items);
        updateCartCount();
        renderCart();

        return true;

    } catch (error) {

        console.error("Lỗi đồng bộ giỏ hàng:", error);

        return false;
    }
}


/* =========================================================
   3. CẬP NHẬT SỐ LƯỢNG TRÊN BIỂU TƯỢNG GIỎ HÀNG
   ========================================================= */

function updateCartCount() {

    const cart = getCart();

    const totalCount = cart.reduce(function (sum, item) {

        if (!item) return sum;

        return sum + Math.max(
            0,
            parseInt(item.quantity, 10) || 0
        );

    }, 0);

    document.querySelectorAll(".cart-count").forEach(function (element) {
        element.textContent = totalCount;
    });
}


/* =========================================================
   4. HIỂN THỊ GIỎ HÀNG
   ========================================================= */

function renderCart() {

    const cartTableBody = document.getElementById("cart-items");

    if (!cartTableBody) return;

    const cart = getCart();

    cartTableBody.innerHTML = "";

    const totalEl = document.getElementById("cart-total");

    if (cart.length === 0) {

        cartTableBody.innerHTML = `
            <tr>
                <td colspan="5"
                    style="text-align:center; padding:20px; color:#666;">
                    Giỏ hàng của bạn đang trống!
                </td>
            </tr>
        `;

        if (totalEl) {
            totalEl.textContent = "0 đ";
        }

        return;
    }

    let totalAll = 0;

    cart.forEach(function (item, index) {

        if (!item) return;

        const price = Number(item.price) || 0;

        const quantity = Math.max(
            0,
            parseInt(item.quantity, 10) || 0
        );

        const itemTotal = price * quantity;

        totalAll += itemTotal;

        const hasStock =
            item.stock !== null &&
            item.stock !== undefined &&
            item.stock !== "" &&
            Number.isInteger(Number(item.stock));

        const row = `
            <tr style="border-bottom:1px solid #eee;">

                <td style="padding:12px;">
                    <div style="display:flex; align-items:center; gap:10px;">

                        <img
                            src="${item.image || ""}"
                            alt="${item.name || "Sản phẩm"}"
                            width="50"
                            height="50"
                            style="object-fit:cover; border-radius:4px;"
                        >

                        <span></span>

                    </div>
                </td>

                <td style="padding:12px; color:#ff4500; font-weight:bold;">
                    ${price.toLocaleString("vi-VN")} đ
                </td>

                <td style="padding:12px;">

                    <input
                        type="number"
                        value="${quantity}"
                        min="1"
                        ${hasStock ? `max="${Number(item.stock)}"` : ""}
                        onchange="updateQuantity(${index}, this.value)"
                        style="width:65px; padding:5px; text-align:center;"
                    >

                    ${hasStock
                ? `<small style="display:block; color:#64748b;">
                            Kho còn: ${Number(item.stock)}
                           </small>`
                : ""
            }

                </td>

                <td style="padding:12px; font-weight:bold;">
                    ${itemTotal.toLocaleString("vi-VN")} đ
                </td>

                <td style="padding:12px;">

                    <button
                        type="button"
                        onclick="removeItem(${index})"
                        style="
                            background:#ff4444;
                            color:white;
                            border:none;
                            padding:6px 12px;
                            border-radius:4px;
                            cursor:pointer;
                        "
                    >
                        Xóa
                    </button>

                </td>

            </tr>
        `;

        cartTableBody.insertAdjacentHTML("beforeend", row);

        const lastRow = cartTableBody.lastElementChild;

        const nameEl = lastRow
            ? lastRow.querySelector("td:first-child span")
            : null;

        if (nameEl) {
            nameEl.textContent = item.name || "Sản phẩm";
        }

        const imageEl = lastRow
            ? lastRow.querySelector("td:first-child img")
            : null;

        if (imageEl) {

            imageEl.onerror = function () {
                imageEl.onerror = null;
                imageEl.removeAttribute("src");
                imageEl.alt = "Không tải được ảnh sản phẩm";
            };
        }
    });

    if (totalEl) {
        totalEl.textContent =
            totalAll.toLocaleString("vi-VN") + " đ";
    }
}


/* =========================================================
   5. CẬP NHẬT SỐ LƯỢNG SẢN PHẨM
   ========================================================= */

async function updateQuantity(index, quantity) {

    const cart = getCart();

    const parsedQty = Number(quantity);

    if (
        !Number.isInteger(parsedQty) ||
        parsedQty < 1 ||
        index < 0 ||
        index >= cart.length
    ) {
        renderCart();
        return;
    }

    const item = cart[index];

    if (!item) {
        renderCart();
        return;
    }

    try {

        await callCartApi("UpdateQuantity", {
            productId: item.id,
            quantity: parsedQty
        });

        await syncCartFromServer();

    } catch (error) {

        alert(error.message);

        await syncCartFromServer();
    }
}


/* =========================================================
   6. XÓA SẢN PHẨM KHỎI GIỎ HÀNG
   ========================================================= */

async function removeItem(index) {

    const cart = getCart();

    if (index < 0 || index >= cart.length) {
        return;
    }

    const item = cart[index];

    if (!item) return;

    try {

        await callCartApi("Remove", {
            productId: item.id
        });

        await syncCartFromServer();

    } catch (error) {

        alert(error.message);

        await syncCartFromServer();
    }
}


/* =========================================================
   7. KHỞI TẠO KHI TRANG TẢI XONG
   ========================================================= */

document.addEventListener("DOMContentLoaded", function () {

    const isAdmin =
        String(window.currentUserRole || "").trim().toLowerCase() === "admin";


    // Admin không được sử dụng chức năng mua hàng
    if (isAdmin) {

        document.querySelectorAll(
            ".add-to-cart-btn, .btn-buy-now, #modalAddToCart"
        ).forEach(function (button) {

            button.style.display = "none";

        });
    }


    // Cập nhật giao diện giỏ hàng ban đầu
    updateCartCount();
    renderCart();


    // Đồng bộ dữ liệu giỏ hàng từ SQL Server
    if (
        window.isUserLoggedIn === true &&
        !isAdmin
    ) {
        syncCartFromServer();
    }


    const modal = document.getElementById("productDetailModal");

    const closeBtn = document.getElementById("closeModalBtn");

    const modalImg = document.getElementById("modalImg");

    const modalTitle = document.getElementById("modalTitle");

    const modalPrice = document.getElementById("modalPrice");

    const modalDesc = document.getElementById("modalDesc");

    const modalAddToCart = document.getElementById("modalAddToCart");

    const nextImgBtn = document.getElementById("nextImgBtn");

    const prevImgBtn = document.getElementById("prevImgBtn");

    const thumbContainer =
        document.getElementById("modalThumbnailList");

    const decreaseBtn =
        document.getElementById("decreaseQtyBtn");

    const increaseBtn =
        document.getElementById("increaseQtyBtn");

    const quantityInput =
        document.getElementById("modalQuantityInput");


    let currentImageList = [];

    let currentImageIndex = 0;


    /* =====================================================
       7.1. CẬP NHẬT ẢNH ĐANG CHỌN TRONG MODAL
       ===================================================== */

    function updateActiveImage() {

        if (modalImg && currentImageList.length > 0) {

            modalImg.src = currentImageList[currentImageIndex];

        }

        if (thumbContainer) {

            thumbContainer.querySelectorAll("img").forEach(
                function (thumb, index) {

                    thumb.style.borderColor =
                        index === currentImageIndex
                            ? "#2563eb"
                            : "#cbd5e1";

                }
            );
        }
    }


    /* =====================================================
       7.2. CHUYỂN ẢNH TIẾP THEO
       ===================================================== */

    if (nextImgBtn) {

        nextImgBtn.addEventListener("click", function () {

            if (currentImageList.length === 0) return;

            currentImageIndex =
                (currentImageIndex + 1) %
                currentImageList.length;

            updateActiveImage();

        });
    }


    /* =====================================================
       7.3. CHUYỂN ẢNH TRƯỚC ĐÓ
       ===================================================== */

    if (prevImgBtn) {

        prevImgBtn.addEventListener("click", function () {

            if (currentImageList.length === 0) return;

            currentImageIndex =
                (currentImageIndex - 1 + currentImageList.length) %
                currentImageList.length;

            updateActiveImage();

        });
    }


    /* =====================================================
       7.4. TÌM KIẾM SẢN PHẨM
       ===================================================== */

    const searchInput =
        document.getElementById("search-input");

    const searchButton =
        document.getElementById("search-button");

    const productItems =
        document.querySelectorAll(".product-item");


    function filterProducts() {

        if (!searchInput) return;

        const keyword =
            searchInput.value.toLowerCase().trim();

        productItems.forEach(function (item) {

            const titleEl =
                item.querySelector(".product-title");

            const btnEl =
                item.querySelector(".add-to-cart-btn");

            const title = titleEl
                ? titleEl.textContent.toLowerCase()
                : "";

            const description = btnEl
                ? (btnEl.getAttribute("data-desc") || "").toLowerCase()
                : "";

            item.style.display =
                title.includes(keyword) ||
                    description.includes(keyword)
                    ? ""
                    : "none";

        });
    }


    if (searchButton && searchInput) {

        searchButton.addEventListener("click", function (event) {

            event.preventDefault();

            filterProducts();

        });

        searchInput.addEventListener("keyup", filterProducts);
    }


    /* =====================================================
       7.5. MỞ MODAL CHI TIẾT SẢN PHẨM
       ===================================================== */

    document.querySelectorAll(".product-card").forEach(
        function (card) {

            card.addEventListener("click", function (event) {

                if (event.target.closest(".add-to-cart-btn")) {
                    return;
                }

                const imgEl =
                    card.querySelector(".product-img");

                const titleEl =
                    card.querySelector(".product-title");

                const priceEl =
                    card.querySelector(".product-price");

                const addToCartBtn =
                    card.querySelector(".add-to-cart-btn");

                if (!imgEl || !titleEl || !priceEl) {
                    return;
                }

                const mainImg = imgEl.src;

                const title =
                    titleEl.textContent.trim();

                const priceText =
                    priceEl.textContent.trim();

                const id = addToCartBtn
                    ? addToCartBtn.getAttribute("data-id")
                    : "";

                const rawPrice = addToCartBtn
                    ? addToCartBtn.getAttribute("data-price")
                    : priceText.replace(/[^\d]/g, "");

                const description = addToCartBtn
                    ? (addToCartBtn.getAttribute("data-desc") || "")
                    : "Sản phẩm thể thao chất lượng cao.";

                const imagesAttr = addToCartBtn
                    ? addToCartBtn.getAttribute("data-images")
                    : "";

                const stock = addToCartBtn
                    ? getProductStock(addToCartBtn)
                    : null;


                // Danh sách ảnh sản phẩm
                currentImageList = imagesAttr
                    ? imagesAttr.split(",").map(function (url) {
                        return url.trim();
                    }).filter(Boolean)
                    : [mainImg];

                if (currentImageList.length === 0) {
                    currentImageList = [mainImg];
                }

                currentImageIndex = 0;

                updateActiveImage();


                if (modalTitle) {
                    modalTitle.textContent = title;
                }

                if (modalPrice) {
                    modalPrice.textContent = priceText;
                }

                if (modalDesc) {
                    modalDesc.textContent = description;
                }


                // Đặt lại số lượng modal
                if (quantityInput) {

                    quantityInput.value = 1;

                    if (stock !== null) {

                        quantityInput.max = stock;

                        quantityInput.disabled = stock <= 0;

                    } else {

                        quantityInput.removeAttribute("max");

                        quantityInput.disabled = false;
                    }
                }


                // Hiển thị danh sách ảnh thu nhỏ
                if (thumbContainer) {

                    thumbContainer.innerHTML = "";

                    currentImageList.forEach(
                        function (imgUrl, index) {

                            const thumb =
                                document.createElement("img");

                            thumb.src = imgUrl;

                            thumb.alt =
                                "Ảnh sản phẩm " + (index + 1);

                            thumb.style.cssText = `
                                width: 60px;
                                height: 60px;
                                object-fit: cover;
                                border-radius: 8px;
                                cursor: pointer;
                                border: 2px solid ${index === 0
                                    ? "#2563eb"
                                    : "#cbd5e1"
                                };
                                transition: all 0.2s;
                            `;

                            thumb.addEventListener(
                                "click",
                                function (e) {

                                    e.stopPropagation();

                                    currentImageIndex = index;

                                    updateActiveImage();

                                }
                            );

                            thumbContainer.appendChild(thumb);

                        }
                    );
                }


                // Truyền dữ liệu sản phẩm sang modal
                if (modalAddToCart) {

                    modalAddToCart.setAttribute(
                        "data-id",
                        id || ""
                    );

                    modalAddToCart.setAttribute(
                        "data-name",
                        title
                    );

                    modalAddToCart.setAttribute(
                        "data-price",
                        rawPrice || ""
                    );

                    modalAddToCart.setAttribute(
                        "data-image",
                        mainImg
                    );

                    if (stock !== null) {

                        modalAddToCart.setAttribute(
                            "data-stock",
                            stock
                        );

                        modalAddToCart.disabled = stock <= 0;

                    } else {

                        modalAddToCart.removeAttribute("data-stock");

                        modalAddToCart.disabled = false;
                    }
                }


                if (modal) {
                    modal.style.display = "flex";
                }

            });
        }
    );


    /* =====================================================
       7.6. ĐÓNG MODAL
       ===================================================== */

    if (closeBtn) {

        closeBtn.addEventListener("click", function () {

            if (modal) {
                modal.style.display = "none";
            }

        });
    }


    window.addEventListener("click", function (event) {

        if (modal && event.target === modal) {
            modal.style.display = "none";
        }

    });


    /* =====================================================
       7.7. TĂNG / GIẢM SỐ LƯỢNG TRONG MODAL
       ===================================================== */

    if (quantityInput && decreaseBtn) {

        decreaseBtn.addEventListener("click", function () {

            const currentValue =
                parseInt(quantityInput.value, 10) || 1;

            const min =
                parseInt(quantityInput.getAttribute("min"), 10) || 1;

            if (currentValue > min) {
                quantityInput.value = currentValue - 1;
            }

        });
    }


    if (quantityInput && increaseBtn) {

        increaseBtn.addEventListener("click", function () {

            const currentValue =
                parseInt(quantityInput.value, 10) || 1;

            const max =
                parseInt(quantityInput.getAttribute("max"), 10);

            if (Number.isInteger(max)) {

                if (currentValue < max) {

                    quantityInput.value = currentValue + 1;

                } else {

                    alert("Bạn đã chọn số lượng tối đa trong kho!");

                }

            } else if (currentValue < 100) {

                quantityInput.value = currentValue + 1;

            }

        });
    }


    /* =====================================================
       7.8. THÊM SẢN PHẨM VÀO GIỎ HÀNG
       ===================================================== */

    document.addEventListener("click", async function (event) {

        const target = event.target;

        if (!(target instanceof Element)) return;

        const button = target.closest(
            ".add-to-cart-btn, .btn-buy-now"
        );

        if (!button) return;

        event.preventDefault();

        event.stopPropagation();


        // Kiểm tra nút
        if (button.disabled) {

            alert("Sản phẩm hiện đã hết hàng.");

            return;
        }


        // Chặn Admin
        if (
            String(window.currentUserRole || "").trim().toLowerCase() === "admin"
        ) {

            alert("Tài khoản Admin không có quyền mua hàng!");

            return;
        }


        // Kiểm tra đăng nhập
        if (window.isUserLoggedIn !== true) {

            alert("Vui lòng đăng nhập tài khoản để thêm sản phẩm vào giỏ hàng!");

            const returnUrl =
                window.location.pathname +
                window.location.search;

            const loginUrl =
                window.loginUrl || "/Account/Login";

            window.location.href =
                loginUrl +
                "?returnUrl=" +
                encodeURIComponent(returnUrl);

            return;
        }


        // Chống nhấn liên tục
        if (button.dataset.processing === "true") {
            return;
        }


        const id = button.getAttribute("data-id");


        // Lấy số lượng
        let quantity = 1;

        if (button.id === "modalAddToCart") {

            if (quantityInput) {
                quantity = Number(quantityInput.value);
            }

        } else if (button.classList.contains("btn-buy-now")) {

            const detailQuantityInput =
                document.getElementById("productQuantity");

            if (detailQuantityInput) {
                quantity = Number(detailQuantityInput.value);
            }
        }


        // Kiểm tra dữ liệu
        if (
            !id ||
            !Number.isInteger(quantity) ||
            quantity < 1 ||
            quantity > 100
        ) {

            alert("Thông tin sản phẩm hoặc số lượng không hợp lệ!");

            return;
        }


        button.dataset.processing = "true";

        try {

            // Gửi yêu cầu thêm sản phẩm lên SQL Server
            const result = await callCartApi("Add", {
                productId: id,
                quantity: quantity
            });


            // Đồng bộ giỏ hàng sau khi SQL xác nhận
            await syncCartFromServer();


            alert(result.message);


            // Đóng modal sau khi thêm thành công
            if (
                modal &&
                modal.style.display === "flex" &&
                button.id === "modalAddToCart"
            ) {

                modal.style.display = "none";

            }

        } catch (error) {

            alert(error.message);

            await syncCartFromServer();

        } finally {

            button.dataset.processing = "false";

        }

    });

});
