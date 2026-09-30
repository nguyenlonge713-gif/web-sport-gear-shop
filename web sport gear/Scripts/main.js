document.addEventListener("DOMContentLoaded", function () {
    updateCartCount();
    renderCart();

    // =========================================================
    // KHAI BÁO CÁC PHẦN TỬ DOM CHUNG
    // =========================================================
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

    // Biến quản lý trạng thái slider ảnh trong modal
    let currentImageList = [];
    let currentImageIndex = 0;

    // Hàm cập nhật hiển thị ảnh theo index hiện tại
    function updateActiveImage() {
        if (modalImg && currentImageList.length > 0) {
            modalImg.src = currentImageList[currentImageIndex];
        }
        // Cập nhật viền cho phần ảnh thu nhỏ bên dưới (nếu có)
        if (thumbContainer) {
            thumbContainer.querySelectorAll("img").forEach((thumb, idx) => {
                thumb.style.borderColor = idx === currentImageIndex ? '#2563eb' : '#cbd5e1';
            });
        }
    }

    // Sự kiện bấm nút Next (sang phải)
    if (nextImgBtn) {
        nextImgBtn.addEventListener("click", function () {
            if (currentImageList.length > 0) {
                currentImageIndex = (currentImageIndex + 1) % currentImageList.length;
                updateActiveImage();
            }
        });
    }

    // Sự kiện bấm nút Prev (sang trái)
    if (prevImgBtn) {
        prevImgBtn.addEventListener("click", function () {
            if (currentImageList.length > 0) {
                currentImageIndex = (currentImageIndex - 1 + currentImageList.length) % currentImageList.length;
                updateActiveImage();
            }
        });
    }

    // =========================================================
    // 1. XỬ LÝ TÍNH NĂNG TÌM KIẾM SẢN PHẨM
    // =========================================================
    const searchInput = document.getElementById("search-input");
    const searchButton = document.getElementById("search-button");
    const productItems = document.querySelectorAll(".product-item");

    function filterProducts() {
        if (!searchInput) return;
        const keyword = searchInput.value.toLowerCase().trim();
        productItems.forEach(item => {
            const titleEl = item.querySelector(".product-title");
            const btnEl = item.querySelector(".add-to-cart-btn");

            const title = titleEl ? titleEl.textContent.toLowerCase() : "";
            const desc = btnEl ? (btnEl.getAttribute("data-desc") || "").toLowerCase() : "";

            if (title.includes(keyword) || desc.includes(keyword)) {
                item.style.display = "block";
            } else {
                item.style.display = "none";
            }
        });
    }

    if (searchButton && searchInput) {
        searchButton.addEventListener("click", filterProducts);
        searchInput.addEventListener("keyup", function () {
            filterProducts();
        });
    }

    // =========================================================
    // 2. XỬ LÝ CLICK MỞ MODAL XEM CHI TIẾT SẢN PHẨM
    // =========================================================
    document.querySelectorAll(".product-card").forEach(card => {
        card.addEventListener("click", function (e) {
            // Nếu người dùng bấm trực tiếp vào nút "Thêm vào giỏ", không bật Modal
            if (e.target.closest(".add-to-cart-btn")) return;

            const imgEl = card.querySelector(".product-img");
            const titleEl = card.querySelector(".product-title");
            const priceEl = card.querySelector(".product-price");
            const addToCartBtn = card.querySelector(".add-to-cart-btn");

            if (!imgEl || !titleEl || !priceEl) return;

            const mainImg = imgEl.src;
            const title = titleEl.innerText;
            const priceText = priceEl.innerText;

            const id = addToCartBtn ? addToCartBtn.getAttribute("data-id") : "";
            const rawPrice = addToCartBtn ? addToCartBtn.getAttribute("data-price") : priceText.replace(/\D/g, "");
            const desc = addToCartBtn ? addToCartBtn.getAttribute("data-desc") : "Sản phẩm thể thao chính hãng chất lượng cao.";

            // Lấy danh sách ảnh từ thuộc tính data-images
            const imagesAttr = addToCartBtn ? addToCartBtn.getAttribute("data-images") : "";
            currentImageList = imagesAttr ? imagesAttr.split(",").map(url => url.trim()) : [mainImg];
            currentImageIndex = 0; // Reset về ảnh đầu tiên khi mở modal

            updateActiveImage();

            if (modalTitle) modalTitle.innerText = title;
            if (modalPrice) modalPrice.innerText = priceText;
            if (modalDesc) modalDesc.innerHTML = `<b>Mô tả chi tiết:</b> ${desc}`;

            // Reset số lượng trong modal về 1 mỗi khi mở
            const quantityInput = document.getElementById('modalQuantityInput');
            if (quantityInput) quantityInput.value = 1;

            // Xây dựng danh sách ảnh thu nhỏ (thumbnails)
            if (thumbContainer) {
                thumbContainer.innerHTML = "";
                currentImageList.forEach((imgUrl, index) => {
                    let thumb = document.createElement("img");
                    thumb.src = imgUrl;
                    thumb.style.cssText = `
                        width: 60px; height: 60px; object-fit: cover; border-radius: 8px; 
                        cursor: pointer; border: 2px solid ${index === 0 ? '#2563eb' : '#cbd5e1'}; 
                        transition: all 0.2s;
                    `;

                    // Click vào ảnh nhỏ sẽ nhảy trực tiếp đến ảnh đó
                    thumb.addEventListener("click", function () {
                        currentImageIndex = index;
                        updateActiveImage();
                    });

                    thumbContainer.appendChild(thumb);
                });
            }

            // Cập nhật thông tin cho nút thêm vào giỏ hàng trong modal
            if (modalAddToCart) {
                modalAddToCart.setAttribute("data-id", id);
                modalAddToCart.setAttribute("data-name", title);
                modalAddToCart.setAttribute("data-price", rawPrice);
                modalAddToCart.setAttribute("data-image", mainImg);
            }

            // Hiển thị Modal
            if (modal) modal.style.display = "flex";
        });
    });

    // Đóng Modal khi bấm nút X
    if (closeBtn) {
        closeBtn.addEventListener("click", function () {
            if (modal) modal.style.display = "none";
        });
    }

    // Đóng Modal khi bấm ra ngoài nền đen
    window.addEventListener("click", function (e) {
        if (e.target === modal) {
            modal.style.display = "none";
        }
    });

    // =========================================================
    // 3. XỬ LÝ NÚT TĂNG GIẢM SỐ LƯỢNG TRONG MODAL
    // =========================================================
    const decreaseBtn = document.getElementById('decreaseQtyBtn');
    const increaseBtn = document.getElementById('increaseQtyBtn');
    const quantityInput = document.getElementById('modalQuantityInput');

    if (quantityInput) {
        if (decreaseBtn) {
            decreaseBtn.addEventListener('click', function () {
                let currentValue = parseInt(quantityInput.value) || 1;
                let min = parseInt(quantityInput.getAttribute('min')) || 1;
                if (currentValue > min) {
                    quantityInput.value = currentValue - 1;
                }
            });
        }

        if (increaseBtn) {
            increaseBtn.addEventListener('click', function () {
                let currentValue = parseInt(quantityInput.value) || 1;
                let max = parseInt(quantityInput.getAttribute('max')) || 100;
                if (currentValue < max) {
                    quantityInput.value = currentValue + 1;
                }
            });
        }
    }

    // =========================================================
    // 4. XỬ LÝ CLICK NÚT "THÊM VÀO GIỎ" (Bắt sự kiện chung)
    // =========================================================
    document.addEventListener("click", function (event) {
        if (event.target && event.target.classList.contains("add-to-cart-btn")) {
            const button = event.target;
            const id = button.getAttribute("data-id");
            const name = button.getAttribute("data-name");
            const price = parseFloat(button.getAttribute("data-price"));
            const image = button.getAttribute("data-image") || "";

            // Lấy số lượng từ input modal nếu thao tác bấm nút nằm trong modal, ngược lại mặc định là 1
            let quantity = 1;
            if (button.id === "modalAddToCart" && quantityInput) {
                quantity = parseInt(quantityInput.value) || 1;
            }

            let cart = JSON.parse(localStorage.getItem("cart")) || [];

            let existingItem = cart.find(item => item.id === id);
            if (existingItem) {
                existingItem.quantity += quantity;
            } else {
                cart.push({ id: id, name: name, price: price, image: image, quantity: quantity });
            }

            localStorage.setItem("cart", JSON.stringify(cart));
            updateCartCount();

            alert(`Đã thêm thành công ${quantity} sản phẩm "${name}" vào giỏ hàng!`);

            // Đóng modal sau khi thêm thành công từ modal
            if (modal && modal.style.display === "flex") {
                modal.style.display = "none";
            }
        }
    });

    // Xóa giỏ hàng khi bấm Tiến hành thanh toán
    const checkoutBtn = document.querySelector('a[href*="Checkout"]');
    if (checkoutBtn) {
        checkoutBtn.addEventListener("click", function () {
            localStorage.removeItem("cart");
            updateCartCount();
        });
    }
});

// =========================================================
// 5. CÁC HÀM XỬ LÝ GIỎ HÀNG (Global functions gọi từ HTML)
// =========================================================
function updateCartCount() {
    let cart = JSON.parse(localStorage.getItem("cart")) || [];
    let totalCount = cart.reduce((sum, item) => sum + item.quantity, 0);

    const cartCountElements = document.querySelectorAll(".cart-count");
    cartCountElements.forEach(el => {
        el.innerText = totalCount;
    });
}

function renderCart() {
    const cartTableBody = document.getElementById("cart-items");
    if (!cartTableBody) return;

    let cart = JSON.parse(localStorage.getItem("cart")) || [];
    cartTableBody.innerHTML = "";

    if (cart.length === 0) {
        cartTableBody.innerHTML = `<tr><td colspan="5" style="text-align: center; padding: 20px; color: #666;">Giỏ hàng của bạn đang trống!</td></tr>`;
        const totalEl = document.getElementById("cart-total");
        if (totalEl) totalEl.innerText = "0 đ";
        return;
    }

    let totalAll = 0;

    cart.forEach((item, index) => {
        let itemTotal = item.price * item.quantity;
        totalAll += itemTotal;

        let row = `
            <tr style="border-bottom: 1px solid #eee;">
                <td style="padding: 12px; display: flex; align-items: center; gap: 10px;">
                    <img src="${item.image}" width="50" height="50" style="object-fit: cover; border-radius: 4px;">
                    <span>${item.name}</span>
                </td>
                <td style="padding: 12px; color: #ff4500; font-weight: bold;">${Number(item.price).toLocaleString()} đ</td>
                <td style="padding: 12px;"><input type="number" value="${item.quantity}" min="1" onchange="updateQuantity(${index}, this.value)" style="width: 50px; padding: 5px; text-align: center;"></td>
                <td style="padding: 12px; font-weight: bold;">${Number(itemTotal).toLocaleString()} đ</td>
                <td style="padding: 12px;"><button onclick="removeItem(${index})" style="background: #ff4444; color: white; border: none; padding: 6px 12px; border-radius: 4px; cursor: pointer;">Xóa</button></td>
            </tr>
        `;
        cartTableBody.innerHTML += row;
    });

    const totalEl = document.getElementById("cart-total");
    if (totalEl) totalEl.innerText = Number(totalAll).toLocaleString() + " đ";
}

function updateQuantity(index, quantity) {
    let cart = JSON.parse(localStorage.getItem("cart")) || [];
    let parsedQty = parseInt(quantity);
    if (parsedQty > 0) {
        cart[index].quantity = parsedQty;
        localStorage.setItem("cart", JSON.stringify(cart));
        renderCart();
        updateCartCount();
    }
}

function removeItem(index) {
    let cart = JSON.parse(localStorage.getItem("cart")) || [];
    cart.splice(index, 1);
    localStorage.setItem("cart", JSON.stringify(cart));
    renderCart();
    updateCartCount();
}

document.addEventListener("DOMContentLoaded", function () {
    const searchInput = document.getElementById("search-input");
    const searchButton = document.getElementById("search-button");
    const productItems = document.querySelectorAll(".product-item");

    function filterProducts() {
        const keyword = searchInput.value.toLowerCase().trim();

        productItems.forEach(item => {
            const titleEl = item.querySelector(".product-title");
            const title = titleEl ? titleEl.textContent.toLowerCase() : "";

            // Kiểm tra nếu tên sản phẩm chứa từ khóa tìm kiếm
            if (title.includes(keyword)) {
                item.style.display = "block"; // Hiển thị nếu khớp
            } else {
                item.style.display = "none";  // Ẩn nếu không khớp
            }
        });
    }

    // Khi bấm nút "Tìm kiếm"
    if (searchButton) {
        searchButton.addEventListener("click", function (e) {
            e.preventDefault();
            filterProducts();
        });
    }

    // Khi người dùng gõ phím trực tiếp vào ô input (tìm kiếm theo thời gian thực)
    if (searchInput) {
        searchInput.addEventListener("keyup", function () {
            filterProducts();
        });
    }
});