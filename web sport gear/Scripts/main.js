
/* =========================================================
   MAIN.JS - GEAR SPORT
   ========================================================= */


/* =========================================================
   HÀM HỖ TRỢ ĐỌC GIỎ HÀNG VÀ TỒN KHO
   ========================================================= */

// Đọc giỏ hàng từ localStorage
function getCart() {
    try {
        const cart = JSON.parse(localStorage.getItem("cart")) || [];
        return Array.isArray(cart) ? cart : [];
    } catch (error) {
        console.error("Không thể đọc giỏ hàng:", error);
        return [];
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
   1. KHỞI TẠO KHI TRANG TẢI XONG
   ========================================================= */

document.addEventListener("DOMContentLoaded", function () {
    updateCartCount();
    renderCart();

    const modal = document.getElementById("productDetailModal");
    const closeBtn = document.getElementById("closeModalBtn");
    const modalImg = document.getElementById("modalImg");
    const modalTitle = document.getElementById("modalTitle");
    const modalPrice = document.getElementById("modalPrice");
    const modalDesc = document.getElementById("modalDesc");
    const modalAddToCart = document.getElementById("modalAddToCart");

    const nextImgBtn = document.getElementById("nextImgBtn");
    const prevImgBtn = document.getElementById("prevImgBtn");
    const thumbContainer = document.getElementById("modalThumbnailList");

    const decreaseBtn = document.getElementById("decreaseQtyBtn");
    const increaseBtn = document.getElementById("increaseQtyBtn");
    const quantityInput = document.getElementById("modalQuantityInput");

    let currentImageList = [];
    let currentImageIndex = 0;


    /* =====================================================
       2. CẬP NHẬT ẢNH ĐANG CHỌN TRONG MODAL
       ===================================================== */

    function updateActiveImage() {
        if (modalImg && currentImageList.length > 0) {
            modalImg.src = currentImageList[currentImageIndex];
        }

        if (thumbContainer) {
            thumbContainer.querySelectorAll("img").forEach(function (thumb, index) {
                thumb.style.borderColor =
                    index === currentImageIndex ? "#2563eb" : "#cbd5e1";
            });
        }
    }

    if (nextImgBtn) {
        nextImgBtn.addEventListener("click", function () {
            if (currentImageList.length === 0) return;

            currentImageIndex =
                (currentImageIndex + 1) % currentImageList.length;

            updateActiveImage();
        });
    }

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
       3. TÌM KIẾM SẢN PHẨM
       ===================================================== */

    const searchInput = document.getElementById("search-input");
    const searchButton = document.getElementById("search-button");
    const productItems = document.querySelectorAll(".product-item");

    function filterProducts() {
        if (!searchInput) return;

        const keyword = searchInput.value.toLowerCase().trim();

        productItems.forEach(function (item) {
            const titleEl = item.querySelector(".product-title");
            const btnEl = item.querySelector(".add-to-cart-btn");

            const title = titleEl
                ? titleEl.textContent.toLowerCase()
                : "";

            const description = btnEl
                ? (btnEl.getAttribute("data-desc") || "").toLowerCase()
                : "";

            item.style.display =
                title.includes(keyword) || description.includes(keyword)
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
       4. MỞ MODAL CHI TIẾT SẢN PHẨM
       ===================================================== */

    document.querySelectorAll(".product-card").forEach(function (card) {
        card.addEventListener("click", function (event) {
            if (event.target.closest(".add-to-cart-btn")) {
                return;
            }

            const imgEl = card.querySelector(".product-img");
            const titleEl = card.querySelector(".product-title");
            const priceEl = card.querySelector(".product-price");
            const addToCartBtn = card.querySelector(".add-to-cart-btn");

            if (!imgEl || !titleEl || !priceEl) return;

            const mainImg = imgEl.src;
            const title = titleEl.textContent.trim();
            const priceText = priceEl.textContent.trim();

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

            if (modalTitle) modalTitle.textContent = title;
            if (modalPrice) modalPrice.textContent = priceText;
            if (modalDesc) modalDesc.textContent = description;

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

            if (thumbContainer) {
                thumbContainer.innerHTML = "";

                currentImageList.forEach(function (imgUrl, index) {
                    const thumb = document.createElement("img");

                    thumb.src = imgUrl;
                    thumb.alt = "Ảnh sản phẩm " + (index + 1);

                    thumb.style.cssText = `
                        width: 60px;
                        height: 60px;
                        object-fit: cover;
                        border-radius: 8px;
                        cursor: pointer;
                        border: 2px solid ${index === 0 ? "#2563eb" : "#cbd5e1"};
                        transition: all 0.2s;
                    `;

                    thumb.addEventListener("click", function (e) {
                        e.stopPropagation();
                        currentImageIndex = index;
                        updateActiveImage();
                    });

                    thumbContainer.appendChild(thumb);
                });
            }

            // Truyền dữ liệu sản phẩm và tồn kho sang modal
            if (modalAddToCart) {
                modalAddToCart.setAttribute("data-id", id || "");
                modalAddToCart.setAttribute("data-name", title);
                modalAddToCart.setAttribute("data-price", rawPrice || "");
                modalAddToCart.setAttribute("data-image", mainImg);

                if (stock !== null) {
                    modalAddToCart.setAttribute("data-stock", stock);
                    modalAddToCart.disabled = stock <= 0;
                } else {
                    modalAddToCart.removeAttribute("data-stock");
                    modalAddToCart.disabled = false;
                }
            }

            if (modal) modal.style.display = "flex";
        });
    });


    /* =====================================================
       5. ĐÓNG MODAL
       ===================================================== */

    if (closeBtn) {
        closeBtn.addEventListener("click", function () {
            if (modal) modal.style.display = "none";
        });
    }

    window.addEventListener("click", function (event) {
        if (modal && event.target === modal) {
            modal.style.display = "none";
        }
    });


    /* =====================================================
       6. TĂNG / GIẢM SỐ LƯỢNG TRONG MODAL
       ===================================================== */

    if (quantityInput && decreaseBtn) {
        decreaseBtn.addEventListener("click", function () {
            const currentValue = parseInt(quantityInput.value, 10) || 1;
            const min = parseInt(quantityInput.getAttribute("min"), 10) || 1;

            if (currentValue > min) {
                quantityInput.value = currentValue - 1;
            }
        });
    }

    if (quantityInput && increaseBtn) {
        increaseBtn.addEventListener("click", function () {
            const currentValue = parseInt(quantityInput.value, 10) || 1;
            const max = parseInt(quantityInput.getAttribute("max"), 10);

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
       7. THÊM SẢN PHẨM VÀO GIỎ HÀNG
       ===================================================== */

    document.addEventListener("click", function (event) {
        const target = event.target;

        if (!(target instanceof Element)) return;

        const button = target.closest(".add-to-cart-btn");
        if (!button) return;

        event.preventDefault();
        event.stopPropagation();

        if (button.disabled) {
            alert("Sản phẩm hiện đã hết hàng.");
            return;
        }

        // Kiểm tra đăng nhập
        if (window.isUserLoggedIn !== true) {
            alert("Vui lòng đăng nhập tài khoản để thêm sản phẩm vào giỏ hàng!");

            const returnUrl =
                window.location.pathname + window.location.search;

            const loginUrl = window.loginUrl || "/Account/Login";

            window.location.href =
                loginUrl + "?returnUrl=" + encodeURIComponent(returnUrl);

            return;
        }

        const id = button.getAttribute("data-id");

        const name =
            button.getAttribute("data-name") ||
            (button.id === "modalAddToCart" && modalTitle
                ? modalTitle.textContent.trim()
                : "");

        const price = Number(button.getAttribute("data-price"));

        const image =
            button.getAttribute("data-image") ||
            button.getAttribute("data-img") ||
            "";

        const stock = getProductStock(button);

        console.log("ĐANG CHẠY HÀM THÊM GIỎ HÀNG", {
            id: id,
            stock: stock
        });

        if (stock === null) {
            alert("Không thể xác định tồn kho sản phẩm. Vui lòng tải lại trang!");
            return;
        }

        if (!id || !name || !Number.isFinite(price) || price <= 0) {
            console.error("Thông tin sản phẩm không hợp lệ:", {
                id: id,
                name: name,
                price: price
            });

            alert("Thông tin sản phẩm không hợp lệ. Vui lòng kiểm tra dữ liệu sản phẩm.");
            return;
        }

        let quantity = 1;

        if (button.id === "modalAddToCart" && quantityInput) {
            quantity = parseInt(quantityInput.value, 10) || 1;
        }

        if (!Number.isInteger(quantity) || quantity < 1) {
            alert("Số lượng sản phẩm không hợp lệ.");
            return;
        }

        // Đọc giỏ hàng và tính số lượng hiện tại
        const cart = getCart();

        console.log("GIỎ HÀNG TRƯỚC KHI THÊM:", cart);

        const existingItem = cart.find(function (item) {
            return item && String(item.id) === String(id);
        });

        const currentQuantity = existingItem
            ? (parseInt(existingItem.quantity, 10) || 0)
            : 0;

        const newTotalQuantity = currentQuantity + quantity;

        console.log("DEBUG TỒN KHO:", {
            id: id,
            stock: stock,
            currentQuantity: currentQuantity,
            quantity: quantity,
            newTotalQuantity: newTotalQuantity
        });

        // KIỂM TRA TỒN KHO TRƯỚC KHI THÊM
        if (stock <= 0) {
            alert("Sản phẩm đã hết hàng!");
            return;
        }

        // Đã đạt số lượng tồn kho tối đa
        if (currentQuantity >= stock) {
            alert(`Không thể thêm! Bạn đã đạt số lượng tối đa (${stock} sản phẩm) trong kho.`);
            return;
        }

        // Số lượng muốn thêm vượt quá tồn kho
        if (newTotalQuantity > stock) {
            const remaining = stock - currentQuantity;

            alert(
                `Không thể thêm số lượng yêu cầu! Kho chỉ còn có thể thêm tối đa ${remaining} sản phẩm.`
            );
            return;
        }

        // Cập nhật sản phẩm đã có hoặc thêm sản phẩm mới
        if (existingItem) {
            existingItem.quantity = newTotalQuantity;
            existingItem.name = name;
            existingItem.price = price;

            if (image) existingItem.image = image;

            existingItem.stock = stock;
        } else {
            cart.push({
                id: id,
                name: name,
                price: price,
                image: image,
                quantity: quantity,
                stock: stock
            });
        }

        // Lưu giỏ hàng
        try {
            localStorage.setItem("cart", JSON.stringify(cart));
        } catch (error) {
            console.error("Không thể lưu giỏ hàng:", error);
            alert("Không thể lưu giỏ hàng. Vui lòng kiểm tra trình duyệt.");
            return;
        }

        updateCartCount();
        renderCart();

        alert(`Đã thêm thành công ${quantity} sản phẩm "${name}" vào giỏ hàng!`);

        if (modal && modal.style.display === "flex" &&
            button.id === "modalAddToCart") {
            modal.style.display = "none";
        }
    });
});


/* =========================================================
   8. CẬP NHẬT SỐ LƯỢNG TRÊN BIỂU TƯỢNG GIỎ HÀNG
   ========================================================= */

function updateCartCount() {
    const cart = getCart();

    // Chỉ tính tổng số lượng, không kiểm tra tồn kho tại đây
    const totalCount = cart.reduce(function (sum, item) {
        if (!item) return sum;

        return sum + Math.max(0, parseInt(item.quantity, 10) || 0);
    }, 0);

    document.querySelectorAll(".cart-count").forEach(function (element) {
        element.textContent = totalCount;
    });
}


/* =========================================================
   9. HIỂN THỊ GIỎ HÀNG
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

        if (totalEl) totalEl.textContent = "0 đ";
        return;
    }

    let totalAll = 0;

    cart.forEach(function (item, index) {
        if (!item) return;

        const price = Number(item.price) || 0;
        const quantity = parseInt(item.quantity, 10) || 0;
        const itemTotal = price * quantity;

        totalAll += itemTotal;

        const hasStock = item.stock !== null &&
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
                ? `<small style="display:block; color:#64748b;">Kho: ${Number(item.stock)}</small>`
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
                        style="background:#ff4444; color:white; border:none; padding:6px 12px; border-radius:4px; cursor:pointer;"
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

        if (nameEl) nameEl.textContent = item.name || "Sản phẩm";

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
        totalEl.textContent = totalAll.toLocaleString("vi-VN") + " đ";
    }
}


/* =========================================================
   10. CẬP NHẬT SỐ LƯỢNG SẢN PHẨM TRONG GIỎ
   ========================================================= */

function updateQuantity(index, quantity) {
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

    // Kiểm tra số lượng với tồn kho đã lưu trong giỏ
    const hasStock = item.stock !== null &&
        item.stock !== undefined &&
        item.stock !== "" &&
        Number.isInteger(Number(item.stock));

    if (hasStock) {
        const stock = Number(item.stock);

        if (stock <= 0) {
            cart.splice(index, 1);
            localStorage.setItem("cart", JSON.stringify(cart));

            alert("Sản phẩm đã hết hàng và được xóa khỏi giỏ hàng.");

            renderCart();
            updateCartCount();
            return;
        }

        if (parsedQty > stock) {
            alert(`Sản phẩm chỉ còn ${stock} sản phẩm trong kho!`);
            item.quantity = stock;

            localStorage.setItem("cart", JSON.stringify(cart));

            renderCart();
            updateCartCount();
            return;
        }
    }

    item.quantity = parsedQty;

    localStorage.setItem("cart", JSON.stringify(cart));

    renderCart();
    updateCartCount();
}


/* =========================================================
   11. XÓA SẢN PHẨM KHỎI GIỎ HÀNG
   ========================================================= */

function removeItem(index) {
    const cart = getCart();

    if (index < 0 || index >= cart.length) return;

    cart.splice(index, 1);

    localStorage.setItem("cart", JSON.stringify(cart));

    renderCart();
    updateCartCount();
}
