import Alpine from 'alpinejs';

// Make Alpine available globally
window.Alpine = Alpine;

// File upload functionality
document.addEventListener('DOMContentLoaded', function() {
    // Initialize Alpine.js
    Alpine.start();

    // File upload drag and drop
    const uploadZones = document.querySelectorAll('.upload-zone');
    
    uploadZones.forEach(zone => {
        zone.addEventListener('dragover', function(e) {
            e.preventDefault();
            this.classList.add('dragover');
        });

        zone.addEventListener('dragleave', function(e) {
            e.preventDefault();
            this.classList.remove('dragover');
        });

        zone.addEventListener('drop', function(e) {
            e.preventDefault();
            this.classList.remove('dragover');
            
            const files = e.dataTransfer.files;
            const fileInput = this.querySelector('input[type="file"]');
            
            if (files.length > 0 && fileInput) {
                fileInput.files = files;
                // Trigger change event
                fileInput.dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
    });

    // File validation
    document.querySelectorAll('input[type="file"]').forEach(input => {
        input.addEventListener('change', function(e) {
            const file = e.target.files[0];
            const errorDiv = document.querySelector('#file-error');
            
            if (file) {
                // Validate file extension
                const validExtensions = ['.db', '.sqlite', '.sqlite3'];
                const fileExtension = file.name.toLowerCase().substring(file.name.lastIndexOf('.'));
                
                if (!validExtensions.includes(fileExtension)) {
                    showError(errorDiv, `Invalid file type. Please select a SQLite database file (.db, .sqlite, .sqlite3)`);
                    this.value = '';
                    return;
                }
                
                // Validate file size (max 50MB)
                const maxSize = 50 * 1024 * 1024; // 50MB
                if (file.size > maxSize) {
                    showError(errorDiv, 'File size too large. Maximum size is 50MB.');
                    this.value = '';
                    return;
                }
                
                hideError(errorDiv);
                updateFileName(file.name);
            }
        });
    });

    // Progress bar functionality
    window.updateProgress = function(percent) {
        const progressBars = document.querySelectorAll('.progress-bar-fill');
        progressBars.forEach(bar => {
            bar.style.width = percent + '%';
        });
    };

    // Copy to clipboard functionality
    window.copyToClipboard = async function(text) {
        try {
            await navigator.clipboard.writeText(text);
            showNotification('Copied to clipboard!', 'success');
        } catch (err) {
            console.error('Failed to copy: ', err);
            showNotification('Failed to copy to clipboard', 'error');
        }
    };

    // Download functionality
    window.downloadFile = function(content, filename, contentType = 'text/plain') {
        const blob = new Blob([content], { type: contentType });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
    };
});

// Helper functions
function showError(errorDiv, message) {
    if (errorDiv) {
        errorDiv.textContent = message;
        errorDiv.classList.remove('hidden');
    }
}

function hideError(errorDiv) {
    if (errorDiv) {
        errorDiv.classList.add('hidden');
    }
}

function updateFileName(fileName) {
    const fileNameDisplay = document.querySelector('#selected-file-name');
    if (fileNameDisplay) {
        fileNameDisplay.textContent = fileName;
        fileNameDisplay.classList.remove('hidden');
    }
}

function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `fixed top-4 right-4 z-50 p-4 rounded-lg shadow-lg ${getNotificationClasses(type)} transition-all duration-300 transform translate-x-full`;
    notification.innerHTML = `
        <div class="flex items-center">
            <span class="mr-2">${getNotificationIcon(type)}</span>
            <span>${message}</span>
            <button onclick="this.parentElement.parentElement.remove()" class="ml-4 text-sm opacity-70 hover:opacity-100">×</button>
        </div>
    `;
    
    document.body.appendChild(notification);
    
    // Animate in
    setTimeout(() => {
        notification.classList.remove('translate-x-full');
    }, 100);
    
    // Auto remove after 3 seconds
    setTimeout(() => {
        notification.classList.add('translate-x-full');
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}

function getNotificationClasses(type) {
    switch (type) {
        case 'success':
            return 'bg-green-500 text-white';
        case 'error':
            return 'bg-red-500 text-white';
        case 'warning':
            return 'bg-yellow-500 text-white';
        default:
            return 'bg-blue-500 text-white';
    }
}

function getNotificationIcon(type) {
    switch (type) {
        case 'success':
            return '✓';
        case 'error':
            return '✗';
        case 'warning':
            return '⚠';
        default:
            return 'ℹ';
    }
}
