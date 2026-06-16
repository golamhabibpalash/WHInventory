// Disable Dropzone auto-discovery immediately at script load (before DOMContentLoaded),
// so it does not try to auto-initialize the <div class="dropzone"> without a URL.
// The uploader is created explicitly in imageDropzone.init().
if (window.Dropzone) {
    Dropzone.autoDiscover = false;
}

const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            productGroupListLookupData: [],
            unitMeasureListLookupData: [],
            brandListLookupData: [],
            mainTitle: null,
            id: '',
            name: '',
            number: '',
            unitPrice: '',
            description: '',
            productGroupId: null,
            unitMeasureId: null,
            brandId: null,
            physical: false,
            isWarrantyApplicable: false,
            barcode: '',
            showBarcodePreview: false,
            imageName: '',
            imagePreviewUrl: '',
            documents: [],
            errors: {
                name: '',
                unitPrice: '',
                productGroupId: '',
                unitMeasureId: '',
                brandId: '',
                image: '',
                documents: ''
            },
            isSubmitting: false,
            productGroupQuickName: '',
            productGroupQuickDescription: '',
            productGroupQuickParentId: '',
            productGroupQuickIsSubmitting: false,
            productGroupQuickErrors: {
                name: ''
            },
            unitMeasureQuickName: '',
            unitMeasureQuickDescription: '',
            unitMeasureQuickIsSubmitting: false,
            unitMeasureQuickErrors: { name: '' },
            brandQuickName: '',
            brandQuickDescription: '',
            brandQuickIsSubmitting: false,
            brandQuickErrors: { name: '' }
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const productGroupQuickModalRef = Vue.ref(null);
        const unitMeasureQuickModalRef = Vue.ref(null);
        const brandQuickModalRef = Vue.ref(null);
        const productGroupQuickParentIdRef = Vue.ref(null);
        const productGroupIdRef = Vue.ref(null);
        const unitMeasureIdRef = Vue.ref(null);
        const brandIdRef = Vue.ref(null);
        const imageUploadRef = Vue.ref(null);
        const docUploadRef = Vue.ref(null);
        const barcodeRef = Vue.ref(null);
        const nameRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const unitPriceRef = Vue.ref(null);

        const validateForm = function () {
            state.errors.name = '';
            state.errors.unitPrice = '';
            state.errors.productGroupId = '';
            state.errors.unitMeasureId = '';
            state.errors.brandId = '';

            let isValid = true;

            if (!state.name) {
                state.errors.name = 'Name is required.';
                isValid = false;
            }
            if (!state.unitPrice) {
                state.errors.unitPrice = 'Unit price is required.';
                isValid = false;
            } else if (!/^\d+(\.\d{1,2})?$/.test(state.unitPrice)) {
                state.errors.unitPrice = 'Unit price must be a numeric value with up to two decimal places.';
                isValid = false;
            }
            if (!state.productGroupId) {
                state.errors.productGroupId = 'ProductGroup is required.';
                isValid = false;
            }
            if (!state.unitMeasureId) {
                state.errors.unitMeasureId = 'UnitMeasure is required.';
                isValid = false;
            }

            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.name = '';
            state.number = '';
            state.unitPrice = '';
            state.description = '';
            state.productGroupId = null;
            state.unitMeasureId = null;
            state.brandId = null;
            state.physical = false;
            state.isWarrantyApplicable = false;
            state.barcode = '';
            state.showBarcodePreview = false;
            state.imageName = '';
            state.imagePreviewUrl = '';
            state.documents = [];
            state.errors = {
                name: '',
                unitPrice: '',
                productGroupId: '',
                unitMeasureId: '',
                brandId: '',
                image: '',
                documents: ''
            };
            docDropzone.reset();
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/Product/GetProductList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (name, unitPrice, physical, isWarrantyApplicable, description, productGroupId, unitMeasureId, brandId, imageName, barcode, createdById) => {
                try {
                    const response = await AxiosManager.post('/Product/CreateProduct', {
                        name, unitPrice, physical, isWarrantyApplicable, description, productGroupId, unitMeasureId, brandId, imageName, barcode, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, name, unitPrice, physical, isWarrantyApplicable, description, productGroupId, unitMeasureId, brandId, imageName, barcode, updatedById) => {
                try {
                    const response = await AxiosManager.post('/Product/UpdateProduct', {
                        id, name, unitPrice, physical, isWarrantyApplicable, description, productGroupId, unitMeasureId, brandId, imageName, barcode, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            uploadProductImage: async (file) => {
                const formData = new FormData();
                formData.append('file', file);
                const response = await AxiosManager.post('/Product/UploadProductImage', formData, {});
                return response;
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/Product/DeleteProduct', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getProductGroupListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/ProductGroup/GetProductGroupList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createProductGroup: async (name, description, createdById, parentId) => {
                try {
                    const response = await AxiosManager.post('/ProductGroup/CreateProductGroup', {
                        name, description, createdById, parentId
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getUnitMeasureListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/UnitMeasure/GetUnitMeasureList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createUnitMeasure: async (name, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/UnitMeasure/CreateUnitMeasure', { name, description, createdById });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getBrandListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Brand/GetBrandList', { params: { isActive: true } });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createBrand: async (name, description, createdById) => {
                try {
                    const response = await AxiosManager.post('/Brand/CreateBrand', { name, description, createdById });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getProductImage: async (imageName) => {
                try {
                    const response = await AxiosManager.get('/FileImage/GetImage?imageName=' + imageName, { responseType: 'blob' });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            uploadDocuments: async (files, moduleId) => {
                const formData = new FormData();
                files.forEach(f => formData.append('files', f));
                formData.append('moduleName', 'Product');
                formData.append('moduleId', moduleId);
                return await AxiosManager.post('/FileDocument/BulkUploadDocuments', formData);
            },
            getDocumentsByModule: async (moduleId) => {
                return await AxiosManager.get('/FileDocument/GetDocumentsByModule', {
                    params: { moduleName: 'Product', moduleId }
                });
            },
            deleteDocument: async (id, deletedById) => {
                return await AxiosManager.post('/FileDocument/DeleteDocument', { id, deletedById });
            },
        };

        const methods = {
            populateProductGroupListLookupData: async () => {
                const response = await services.getProductGroupListLookupData();
                const groups = response?.data?.content?.data ?? [];

                // Build a full hierarchical path (e.g. "A > B > C") for each group
                // by walking its parent chain. Guards against missing/cyclic parents.
                const byId = new Map(groups.map(g => [g.id, g]));
                const buildPath = (group) => {
                    const segments = [];
                    const visited = new Set();
                    let current = group;
                    while (current && !visited.has(current.id)) {
                        visited.add(current.id);
                        segments.unshift(current.name);
                        current = current.parentId ? byId.get(current.parentId) : null;
                    }
                    return segments.join(' > ');
                };

                state.productGroupListLookupData = groups
                    .map(g => ({ ...g, hierarchyPath: buildPath(g) }))
                    .sort((a, b) => a.hierarchyPath.localeCompare(b.hierarchyPath));
            },
            populateUnitMeasureListLookupData: async () => {
                const response = await services.getUnitMeasureListLookupData();
                state.unitMeasureListLookupData = response?.data?.content?.data;
            },
            populateBrandListLookupData: async () => {
                const response = await services.getBrandListLookupData();
                state.brandListLookupData = response?.data?.content?.data;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
            loadImagePreview: async (imageName) => {
                if (!imageName) {
                    state.imagePreviewUrl = '';
                    return;
                }
                try {
                    const response = await services.getProductImage(imageName);
                    state.imagePreviewUrl = URL.createObjectURL(response.data);
                } catch {
                    state.imagePreviewUrl = '';
                }
            },
            loadDocuments: async (moduleId) => {
                try {
                    const response = await services.getDocumentsByModule(moduleId);
                    state.documents = response?.data?.content?.data ?? [];
                } catch {
                    state.documents = [];
                }
            },
        };

        const productGroupListLookup = {
            obj: null,
            create: () => {
                if (state.productGroupListLookupData && Array.isArray(state.productGroupListLookupData)) {
                    productGroupListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.productGroupListLookupData,
                        fields: { value: 'id', text: 'hierarchyPath' },
                        placeholder: 'Select a Product Group',
                        popupHeight: '200px',
                        allowFiltering: true,
                        change: (e) => {
                            state.productGroupId = e.value;
                        }
                    });
                    productGroupListLookup.obj.appendTo(productGroupIdRef.value);
                } else {
                }
            },
            refresh: () => {
                if (productGroupListLookup.obj) {
                    productGroupListLookup.obj.value = state.productGroupId;
                }
            },
        };

        const unitMeasureListLookup = {
            obj: null,
            create: () => {
                if (state.unitMeasureListLookupData && Array.isArray(state.unitMeasureListLookupData)) {
                    unitMeasureListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.unitMeasureListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select a Unit Measure',
                        popupHeight: '200px',
                        change: (e) => {
                            state.unitMeasureId = e.value;
                        }
                    });
                    unitMeasureListLookup.obj.appendTo(unitMeasureIdRef.value);
                } else {
                }
            },
            refresh: () => {
                if (unitMeasureListLookup.obj) {
                    unitMeasureListLookup.obj.value = state.unitMeasureId;
                }
            },
        };

        const brandListLookup = {
            obj: null,
            create: () => {
                if (state.brandListLookupData && Array.isArray(state.brandListLookupData)) {
                    brandListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.brandListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: '-- No Brand --',
                        popupHeight: '200px',
                        allowFiltering: true,
                        showClearButton: true,
                        change: (e) => {
                            state.brandId = e.value ?? null;
                        }
                    });
                    brandListLookup.obj.appendTo(brandIdRef.value);
                } else {
                }
            },
            refresh: () => {
                if (brandListLookup.obj) {
                    brandListLookup.obj.value = state.brandId;
                }
            },
        };

        const nameText = {
            obj: null,
            create: () => {
                nameText.obj = new ej.inputs.TextBox({
                    placeholder: 'Enter Name',
                });
                nameText.obj.appendTo(nameRef.value);
            },
            refresh: () => {
                if (nameText.obj) {
                    nameText.obj.value = state.name;
                }
            }
        };

        const numberText = {
            obj: null,
            create: () => {
                numberText.obj = new ej.inputs.TextBox({
                    placeholder: '[auto]',
                    readonly: true
                });
                numberText.obj.appendTo(numberRef.value);
            },
            refresh: () => {
                if (numberText.obj) {
                    numberText.obj.value = state.number;
                }
            }
        };

        const unitPriceNumber = {
            obj: null,
            create: () => {
                unitPriceNumber.obj = new ej.inputs.NumericTextBox({
                    format: 'n2',
                    placeholder: 'Enter Unit Price',
                    min: 0,
                    step: 0.01,
                    validateDecimalOnType: true
                });
                unitPriceNumber.obj.appendTo(unitPriceRef.value);
            },
            refresh: () => {
                if (unitPriceNumber.obj) {
                    unitPriceNumber.obj.value = state.unitPrice;
                }
            }
        };

        Vue.watch(
            () => state.name,
            (newVal, oldVal) => {
                state.errors.name = '';
                nameText.refresh();
            }
        );

        Vue.watch(
            () => state.number,
            (newVal, oldVal) => {
                numberText.refresh();
            }
        );

        Vue.watch(
            () => state.unitPrice,
            (newVal, oldVal) => {
                state.errors.unitPrice = '';
                unitPriceNumber.refresh();
            }
        );

        Vue.watch(
            () => state.productGroupId,
            (newVal, oldVal) => {
                state.errors.productGroupId = '';
                productGroupListLookup.refresh();
            }
        );

        Vue.watch(
            () => state.unitMeasureId,
            (newVal, oldVal) => {
                state.errors.unitMeasureId = '';
                unitMeasureListLookup.refresh();
            }
        );

        Vue.watch(
            () => state.brandId,
            (newVal, oldVal) => {
                state.errors.brandId = '';
                brandListLookup.refresh();
            }
        );

        const imageDropzone = {
            initialized: false,
            allowedExtensions: ['png', 'jpg', 'jpeg'],
            maxFileSizeInBytes: 1 * 1024 * 1024, // 1 MB
            init: () => {
                if (imageDropzone.initialized || !imageUploadRef.value) return;
                imageDropzone.initialized = true;
                Dropzone.autoDiscover = false;
                new Dropzone(imageUploadRef.value, {
                    url: 'api/Product/UploadProductImage',
                    paramName: 'file',
                    maxFiles: 1,
                    autoProcessQueue: false,
                    addRemoveLinks: false,
                    dictDefaultMessage: 'Drop product image here or click to upload',
                    init: function () {
                        this.on('addedfile', async function (file) {
                            state.errors.image = '';

                            const extension = (file.name.split('.').pop() || '').toLowerCase();
                            if (!imageDropzone.allowedExtensions.includes(extension)) {
                                state.errors.image = 'Only PNG and JPG images are allowed.';
                                this.removeFile(file);
                                return;
                            }
                            if (file.size > imageDropzone.maxFileSizeInBytes) {
                                state.errors.image = 'Image must be 1 MB or smaller.';
                                this.removeFile(file);
                                return;
                            }

                            try {
                                const response = await services.uploadProductImage(file);
                                const imageName = response?.data?.content?.imageName;
                                if (imageName) {
                                    state.imageName = imageName;
                                    await methods.loadImagePreview(imageName);
                                } else {
                                    state.errors.image = 'Image upload failed. Please try again.';
                                }
                            } catch (error) {
                                state.errors.image = error.response?.data?.message ?? 'Image upload failed. Please try again.';
                            } finally {
                                this.removeFile(file);
                            }
                        });
                    }
                });
            },
        };

        const docDropzone = {
            obj: null,
            allowedExtensions: ['pdf', 'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx'],
            maxFileSizeInBytes: 25 * 1024 * 1024,
            init: () => {
                if (docDropzone.obj || !docUploadRef.value) return;
                Dropzone.autoDiscover = false;
                docDropzone.obj = new Dropzone(docUploadRef.value, {
                    url: '#',
                    paramName: 'files',
                    maxFiles: 20,
                    autoProcessQueue: false,
                    addRemoveLinks: true,
                    dictDefaultMessage: 'Drop files here or click to upload (PDF, Word, Excel, PowerPoint)',
                    dictRemoveFile: 'Remove',
                    init: function () {
                        this.on('addedfile', async function (file) {
                            state.errors.documents = '';
                            const extension = (file.name.split('.').pop() || '').toLowerCase();
                            if (!docDropzone.allowedExtensions.includes(extension)) {
                                state.errors.documents = `File type '.${extension}' is not allowed.`;
                                this.removeFile(file);
                                return;
                            }
                            if (file.size > docDropzone.maxFileSizeInBytes) {
                                state.errors.documents = `"${file.name}" exceeds the 25 MB limit.`;
                                this.removeFile(file);
                                return;
                            }

                            if (!state.id) return;

                            try {
                                const response = await services.uploadDocuments([file], state.id);
                                if (response?.data?.code === 200) {
                                    await methods.loadDocuments(state.id);
                                } else {
                                    state.errors.documents = response?.data?.message ?? 'Upload failed.';
                                }
                            } catch (error) {
                                state.errors.documents = error.response?.data?.message ?? 'Upload failed.';
                            } finally {
                                this.removeFile(file);
                            }
                        });
                    }
                });
            },
            reset: () => {
                if (docDropzone.obj) {
                    docDropzone.obj.removeAllFiles(true);
                }
            },
        };

        const productGroupQuickParentListLookup = {
            obj: null,
            create: () => {
                productGroupQuickParentListLookup.obj = new ej.dropdowns.DropDownList({
                    dataSource: state.productGroupListLookupData,
                    fields: { value: 'id', text: 'hierarchyPath' },
                    placeholder: '-- No Parent --',
                    popupHeight: '200px',
                    allowFiltering: true,
                    showClearButton: true,
                    change: (e) => {
                        state.productGroupQuickParentId = e.value ?? '';
                    }
                });
                productGroupQuickParentListLookup.obj.appendTo(productGroupQuickParentIdRef.value);
            },
            refresh: () => {
                if (productGroupQuickParentListLookup.obj) {
                    productGroupQuickParentListLookup.obj.dataSource = state.productGroupListLookupData;
                    productGroupQuickParentListLookup.obj.value = state.productGroupQuickParentId || null;
                }
            }
        };

        const productGroupQuickModal = {
            obj: null,
            create: () => {
                productGroupQuickModal.obj = new bootstrap.Modal(productGroupQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const unitMeasureQuickModal = {
            obj: null,
            create: () => {
                unitMeasureQuickModal.obj = new bootstrap.Modal(unitMeasureQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const brandQuickModal = {
            obj: null,
            create: () => {
                brandQuickModal.obj = new bootstrap.Modal(brandQuickModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const handler = {
            openProductGroupQuickCreate: () => {
                state.productGroupQuickName = '';
                state.productGroupQuickDescription = '';
                state.productGroupQuickParentId = '';
                state.productGroupQuickErrors = { name: '' };
                productGroupQuickParentListLookup.refresh();
                productGroupQuickModal.obj.show();
            },
            closeProductGroupQuickCreate: () => {
                productGroupQuickModal.obj.hide();
            },
            submitProductGroupQuickCreate: async () => {
                state.productGroupQuickErrors = { name: '' };
                if (!state.productGroupQuickName) {
                    state.productGroupQuickErrors.name = 'Name is required.';
                    return;
                }
                try {
                    state.productGroupQuickIsSubmitting = true;
                    const parentId = state.productGroupQuickParentId === '' ? null : state.productGroupQuickParentId;
                    const response = await services.createProductGroup(
                        state.productGroupQuickName,
                        state.productGroupQuickDescription,
                        StorageManager.getUserId(),
                        parentId
                    );
                    if (response.data.code === 200) {
                        const newGroup = response.data.content.data;
                        await methods.populateProductGroupListLookupData();
                        productGroupListLookup.obj.setProperties({
                            dataSource: state.productGroupListLookupData,
                            value: newGroup.id
                        });
                        state.productGroupId = newGroup.id;
                        productGroupQuickModal.obj.hide();
                    } else {
                        state.productGroupQuickErrors.name = response.data.message ?? 'Failed to create product group.';
                    }
                } catch (error) {
                    state.productGroupQuickErrors.name = error.response?.data?.message ?? 'An error occurred.';
                } finally {
                    state.productGroupQuickIsSubmitting = false;
                }
            },
            openUnitMeasureQuickCreate: () => {
                state.unitMeasureQuickName = '';
                state.unitMeasureQuickDescription = '';
                state.unitMeasureQuickErrors = { name: '' };
                unitMeasureQuickModal.obj.show();
            },
            closeUnitMeasureQuickCreate: () => {
                unitMeasureQuickModal.obj.hide();
            },
            submitUnitMeasureQuickCreate: async () => {
                state.unitMeasureQuickErrors = { name: '' };
                if (!state.unitMeasureQuickName.trim()) {
                    state.unitMeasureQuickErrors.name = 'Name is required.';
                    return;
                }
                try {
                    state.unitMeasureQuickIsSubmitting = true;
                    const response = await services.createUnitMeasure(
                        state.unitMeasureQuickName.trim(),
                        state.unitMeasureQuickDescription,
                        StorageManager.getUserId()
                    );
                    if (response.data.code === 200) {
                        const newId = response.data.content.data.id;
                        await methods.populateUnitMeasureListLookupData();
                        unitMeasureListLookup.obj.setProperties({ dataSource: state.unitMeasureListLookupData, value: newId });
                        state.unitMeasureId = newId;
                        unitMeasureQuickModal.obj.hide();
                        Swal.fire({ icon: 'success', title: 'Unit Measure Created', timer: 1500, showConfirmButton: false });
                    } else {
                        state.unitMeasureQuickErrors.name = response.data.message ?? 'Failed to create unit measure.';
                    }
                } catch (error) {
                    state.unitMeasureQuickErrors.name = error.response?.data?.message ?? 'An error occurred.';
                } finally {
                    state.unitMeasureQuickIsSubmitting = false;
                }
            },
            openBrandQuickCreate: () => {
                state.brandQuickName = '';
                state.brandQuickDescription = '';
                state.brandQuickErrors = { name: '' };
                brandQuickModal.obj.show();
            },
            closeBrandQuickCreate: () => {
                brandQuickModal.obj.hide();
            },
            submitBrandQuickCreate: async () => {
                state.brandQuickErrors = { name: '' };
                if (!state.brandQuickName.trim()) {
                    state.brandQuickErrors.name = 'Name is required.';
                    return;
                }
                try {
                    state.brandQuickIsSubmitting = true;
                    const response = await services.createBrand(
                        state.brandQuickName.trim(),
                        state.brandQuickDescription,
                        StorageManager.getUserId()
                    );
                    if (response.data.code === 200) {
                        const newBrand = response.data.content.data;
                        await methods.populateBrandListLookupData();
                        brandListLookup.obj.setProperties({ dataSource: state.brandListLookupData, value: newBrand.id });
                        state.brandId = newBrand.id;
                        brandQuickModal.obj.hide();
                        Swal.fire({ icon: 'success', title: 'Brand Created', timer: 1500, showConfirmButton: false });
                    } else {
                        state.brandQuickErrors.name = response.data.message ?? 'Failed to create brand.';
                    }
                } catch (error) {
                    state.brandQuickErrors.name = error.response?.data?.message ?? 'An error occurred.';
                } finally {
                    state.brandQuickIsSubmitting = false;
                }
            },
            renderBarcode: () => {
                if (!state.barcode || !barcodeRef.value) return;
                try {
                    JsBarcode(barcodeRef.value, state.barcode, {
                        format: 'CODE128',
                        displayValue: true,
                        fontSize: 14,
                        height: 60,
                        margin: 10
                    });
                    state.showBarcodePreview = true;
                } catch {
                    state.showBarcodePreview = false;
                }
            },
            formatFileSize: (bytes) => {
                if (!bytes) return '—';
                if (bytes < 1024) return bytes + ' B';
                if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
                return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
            },
            deleteDocument: async (docId) => {
                const confirm = await Swal.fire({
                    title: 'Remove attachment?',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'Yes, remove',
                    cancelButtonText: 'Cancel'
                });
                if (!confirm.isConfirmed) return;
                try {
                    await services.deleteDocument(docId, StorageManager.getUserId());
                    await methods.loadDocuments(state.id);
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'Failed to remove attachment', text: error.response?.data?.message ?? 'Please try again.' });
                }
            },
            handleSubmit: async function () {
                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 300));

                    if (!validateForm()) {
                        return;
                    }

                    const brandId = state.brandId === '' ? null : state.brandId;

                    const response = state.id === ''
                        ? await services.createMainData(state.name, state.unitPrice, state.physical, state.isWarrantyApplicable, state.description, state.productGroupId, state.unitMeasureId, brandId, state.imageName, state.barcode || null, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.name, state.unitPrice, state.physical, state.isWarrantyApplicable, state.description, state.productGroupId, state.unitMeasureId, brandId, state.imageName, state.barcode || null, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = 'Edit Product';
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.name = response?.data?.content?.data.name ?? '';
                            state.unitPrice = response?.data?.content?.data.unitPrice ?? '';
                            state.description = response?.data?.content?.data.description ?? '';
                            state.productGroupId = response?.data?.content?.data.productGroupId ?? '';
                            state.unitMeasureId = response?.data?.content?.data.unitMeasureId ?? '';
                            state.brandId = response?.data?.content?.data.brandId ?? '';
                            state.physical = response?.data?.content?.data.physical ?? false;
                            state.isWarrantyApplicable = response?.data?.content?.data.isWarrantyApplicable ?? false;
                            state.barcode = response?.data?.content?.data.barcode ?? '';
                            state.imageName = response?.data?.content?.data.imageName ?? '';
                            await methods.loadDocuments(state.id);

                            Swal.fire({
                                icon: 'success',
                                title: 'Save Successful',
                                text: 'Form will be closed...',
                                timer: 2000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                            }, 2000);

                        } else {
                            Swal.fire({
                                icon: 'success',
                                title: 'Delete Successful',
                                text: 'Form will be closed...',
                                timer: 2000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                                resetFormState();
                            }, 2000);
                        }

                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: state.deleteMode ? 'Delete Failed' : 'Save Failed',
                            text: response.data.message ?? 'Please check your data.',
                            confirmButtonText: 'Try Again'
                        });
                    }

                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'An Error Occurred',
                        text: error.response?.data?.message ?? 'Please try again.',
                        confirmButtonText: 'OK'
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Products']);
                await SecurityManager.validateToken();

                await Promise.all([
                    methods.populateMainData(),
                    methods.populateProductGroupListLookupData(),
                    methods.populateUnitMeasureListLookupData(),
                    methods.populateBrandListLookupData(),
                ]);
                await mainGrid.create(state.mainData);
                productGroupListLookup.create();
                unitMeasureListLookup.create();
                brandListLookup.create();

                nameText.create();
                numberText.create();
                unitPriceNumber.create();

                mainModal.create();
                productGroupQuickModal.create();
                productGroupQuickParentListLookup.create();
                unitMeasureQuickModal.create();
                brandQuickModal.create();
                imageDropzone.init();
                docDropzone.init();
                mainModalRef.value?.addEventListener('hidden.bs.modal', () => {
                    resetFormState();
                });

            } catch (e) {
            } finally {
                
            }
        });

        Vue.onUnmounted(() => {
            mainModalRef.value?.removeEventListener('hidden.bs.modal', resetFormState);
        });

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: '240px',
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowGrouping: true,
                    groupSettings: {
                        columns: ['productGroupName']
                    },
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    sortSettings: { columns: [{ field: 'createdAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'number', headerText: 'Number', width: 200, minWidth: 200 },
                        { field: 'name', headerText: 'Name', width: 200, minWidth: 200 },
                        { field: 'productGroupName', headerText: 'Product Group', width: 150, minWidth: 150 },
                        { field: 'brandName', headerText: 'Brand', width: 150, minWidth: 150 },
                        { field: 'unitPrice', headerText: 'Unit Price', width: 150, minWidth: 150, format: 'N2' },
                        { field: 'unitMeasureName', headerText: 'Unit Measure', width: 150, minWidth: 150 },
                        { field: 'barcode', headerText: 'Barcode', width: 150, minWidth: 150 },
                        { field: 'physical', headerText: 'Physical Product', width: 140, minWidth: 140, textAlign: 'Center', type: 'boolean', displayAsCheckBox: true },
                        { field: 'isWarrantyApplicable', headerText: 'Warranty', width: 120, minWidth: 120, textAlign: 'Center', type: 'boolean', displayAsCheckBox: true },
                        { field: 'createdAtUtc', headerText: 'Created At UTC', width: 150, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Add', tooltipText: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', tooltipText: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                        { type: 'Separator' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        mainGrid.obj.autoFitColumns(['number', 'name', 'productGroupName', 'brandName', 'unitPrice', 'unitMeasureName', 'physical', 'isWarrantyApplicable', 'createdAtUtc']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        }
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            mainGrid.obj.excelExport();
                        }

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = 'Add Product';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Product';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.name = selectedRecord.name ?? '';
                                state.unitPrice = selectedRecord.unitPrice ?? '';
                                state.description = selectedRecord.description ?? '';
                                state.productGroupId = selectedRecord.productGroupId ?? '';
                                state.unitMeasureId = selectedRecord.unitMeasureId ?? '';
                                state.brandId = selectedRecord.brandId ?? '';
                                state.physical = selectedRecord.physical ?? false;
                                state.isWarrantyApplicable = selectedRecord.isWarrantyApplicable ?? false;
                                state.barcode = selectedRecord.barcode ?? '';
                                state.imageName = selectedRecord.imageName ?? '';
                                await Promise.all([
                                    methods.loadImagePreview(state.imageName),
                                    methods.loadDocuments(state.id)
                                ]);
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Product?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.name = selectedRecord.name ?? '';
                                state.unitPrice = selectedRecord.unitPrice ?? '';
                                state.description = selectedRecord.description ?? '';
                                state.productGroupId = selectedRecord.productGroupId ?? '';
                                state.unitMeasureId = selectedRecord.unitMeasureId ?? '';
                                state.brandId = selectedRecord.brandId ?? '';
                                state.physical = selectedRecord.physical ?? false;
                                state.isWarrantyApplicable = selectedRecord.isWarrantyApplicable ?? false;
                                state.barcode = selectedRecord.barcode ?? '';
                                state.imageName = selectedRecord.imageName ?? '';
                                await methods.loadImagePreview(state.imageName);
                                mainModal.obj.show();
                            }
                        }
                    }
                });

                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        return {
            mainGridRef,
            mainModalRef,
            productGroupQuickModalRef,
            productGroupQuickParentIdRef,
            unitMeasureQuickModalRef,
            brandQuickModalRef,
            productGroupIdRef,
            unitMeasureIdRef,
            brandIdRef,
            imageUploadRef,
            docUploadRef,
            barcodeRef,
            nameRef,
            numberRef,
            unitPriceRef,
            state,
            handler,
        };
    }
};

Vue.createApp(App).mount('#app');