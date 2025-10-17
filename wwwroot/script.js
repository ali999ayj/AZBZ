const jsonHeaders = { 'Content-Type': 'application/json' };

function requestJson(url, method, payload) {
    const options = { method, headers: jsonHeaders };
    if (payload !== undefined) {
        options.body = JSON.stringify(payload);
    }
    return fetch(url, options);
}

function requestDeleteWithQuery(url, params) {
    const cleaned = {};
    Object.entries(params ?? {}).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
            cleaned[key] = value;
        }
    });
    const query = new URLSearchParams(cleaned);
    const suffix = query.toString();
    return fetch(suffix ? `${url}?${suffix}` : url, { method: 'DELETE' });
}

const endpoints = {
    university: {
        create: (values) => requestJson('/api/universities/create-by-name', 'POST', values.universityName),
        rename: (values) => requestJson('/api/universities/rename', 'PUT', {
            currentName: values.universityName,
            newName: values.newName
        }),
        delete: (values) => fetch(`/api/universities/by-name/${encodeURIComponent(values.universityName)}`, {
            method: 'DELETE'
        })
    },
    college: {
        create: (values) => requestJson('/api/colleges/create-by-name', 'POST', {
            universityName: values.universityName,
            collegeName: values.collegeName
        }),
        rename: (values) => requestJson('/api/colleges/rename', 'PUT', {
            universityName: values.universityName,
            currentName: values.collegeName,
            newName: values.newName
        }),
        delete: (values) => requestDeleteWithQuery('/api/colleges/by-name', {
            universityName: values.universityName,
            collegeName: values.collegeName
        })
    },
    department: {
        create: (values) => requestJson('/api/departments/create-by-name', 'POST', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName
        }),
        rename: (values) => requestJson('/api/departments/rename', 'PUT', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            currentName: values.departmentName,
            newName: values.newName
        }),
        delete: (values) => requestDeleteWithQuery('/api/departments/by-name', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName
        })
    },
    stage: {
        create: (values) => requestJson('/api/stages/create-by-name', 'POST', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            stageName: values.stageName
        }),
        rename: (values) => requestJson('/api/stages/rename', 'PUT', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            currentName: values.stageName,
            newName: values.newName
        }),
        delete: (values) => requestDeleteWithQuery('/api/stages/by-name', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            stageName: values.stageName
        })
    },
    group: {
        create: (values) => requestJson('/api/groups/create-by-name', 'POST', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            stageName: values.stageName ?? null,
            groupName: values.groupName,
            kind: values.kind
        }),
        rename: (values) => requestJson('/api/groups/rename', 'PUT', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            stageName: values.stageName ?? null,
            kind: values.kind,
            currentName: values.groupName,
            newName: values.newName
        }),
        delete: (values) => requestDeleteWithQuery('/api/groups/by-name', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            stageName: values.stageName,
            kind: values.kind,
            groupName: values.groupName
        })
    },
    course: {
        create: (values) => requestJson('/api/courses/create-by-name', 'POST', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            courseName: values.courseName
        }),
        rename: (values) => requestJson('/api/courses/rename', 'PUT', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            currentName: values.courseName,
            newName: values.newName
        }),
        delete: (values) => requestDeleteWithQuery('/api/courses/by-name', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            courseName: values.courseName
        })
    },
    instructor: {
        create: (values) => requestJson('/api/instructors/create-by-name', 'POST', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            instructorName: values.instructorName
        }),
        rename: (values) => requestJson('/api/instructors/rename', 'PUT', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            currentName: values.instructorName,
            newName: values.newName
        }),
        delete: (values) => requestDeleteWithQuery('/api/instructors/by-name', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            instructorName: values.instructorName
        })
    },
    room: {
        create: (values) => requestJson('/api/rooms/create-by-name', 'POST', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            roomName: values.roomName
        }),
        rename: (values) => requestJson('/api/rooms/rename', 'PUT', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            currentName: values.roomName,
            newName: values.newName
        }),
        delete: (values) => requestDeleteWithQuery('/api/rooms/by-name', {
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName,
            roomName: values.roomName
        })
    }
};

function serializeForm(form) {
    const data = new FormData(form);
    const result = {};
    data.forEach((value, key) => {
        if (value !== null && value !== '') {
            result[key] = value;
        }
    });
    return result;
}

function showResult(targetId, ok, payload) {
    const element = document.getElementById(targetId);
    element.textContent = ok ? JSON.stringify(payload, null, 2) : `خطأ: ${payload}`;
    element.classList.toggle('error', !ok);
}

async function executeEndpoint(handler, values) {
    const response = await handler(values);
    const text = await response.text();
    let payload;
    try {
        payload = text ? JSON.parse(text) : null;
    } catch {
        payload = text;
    }
    return { ok: response.ok, payload: payload ?? (response.ok ? 'تم التنفيذ بنجاح' : text) };
}

function attachEntityForm(formId, key) {
    const form = document.getElementById(formId);
    const newNameField = form.querySelector('[data-field="newName"]');

    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        const values = serializeForm(form);
        const { ok, payload } = await executeEndpoint(endpoints[key].create, values);
        showResult('entity-result', ok, payload);
        if (ok) {
            form.reset();
            newNameField?.classList.add('hidden');
        }
    });

    form.querySelectorAll('button[data-action]').forEach((button) => {
        button.addEventListener('click', async (event) => {
            const action = button.getAttribute('data-action');
            if (action === 'create') {
                return;
            }
            event.preventDefault();
            const handler = endpoints[key][action];
            if (!handler) {
                return;
            }
            const values = serializeForm(form);
            if (action === 'rename' && !values.newName) {
                newNameField?.classList.remove('hidden');
                newNameField?.querySelector('input')?.focus();
                showResult('entity-result', false, 'يرجى إدخال الاسم الجديد.');
                return;
            }
            const { ok, payload } = await executeEndpoint(handler, values);
            showResult('entity-result', ok, payload);
            if (ok && action !== 'delete') {
                form.reset();
                newNameField?.classList.add('hidden');
            }
        });
    });

    if (newNameField) {
        const newNameInput = newNameField.querySelector('input');
        newNameInput?.addEventListener('focus', () => newNameField.classList.remove('hidden'));
        newNameInput?.addEventListener('input', () => {
            if (newNameInput.value) {
                newNameField.classList.remove('hidden');
            }
        });
    }
}

function attachScheduleForms() {
    const scheduleForm = document.getElementById('form-schedule-entry');
    const overrideForm = document.getElementById('form-schedule-override');

    scheduleForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        const values = serializeForm(scheduleForm);
        const { ok, payload } = await executeEndpoint((data) => requestJson('/api/schedule/entries/by-name', 'POST', data), values);
        showResult('schedule-result', ok, payload);
        if (ok) {
            scheduleForm.reset();
        }
    });

    overrideForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        const values = serializeForm(overrideForm);
        const { ok, payload } = await executeEndpoint((data) => requestJson('/api/schedule/override/by-name', 'POST', data), values);
        showResult('schedule-result', ok, payload);
        if (ok) {
            overrideForm.reset();
        }
    });
}

function attachImportForms() {
    const studentsForm = document.getElementById('form-import-students');
    const attendanceForm = document.getElementById('form-import-attendance');

    studentsForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        const formData = new FormData(studentsForm);
        const response = await fetch('/api/import/students-excel', {
            method: 'POST',
            body: formData
        });
        const text = await response.text();
        showResult('import-result', response.ok, text || 'تم الاستيراد بنجاح');
        if (response.ok) {
            studentsForm.reset();
        }
    });

    attendanceForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        const formData = new FormData(attendanceForm);
        const response = await fetch('/api/import/attendance-excel', {
            method: 'POST',
            body: formData
        });
        const text = await response.text();
        showResult('import-result', response.ok, text || 'تم الاستيراد بنجاح');
        if (response.ok) {
            attendanceForm.reset();
        }
    });
}

function attachReportForms() {
    const scheduleForm = document.getElementById('form-report-schedule');
    const attendanceForm = document.getElementById('form-report-attendance');

    scheduleForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        const values = serializeForm(scheduleForm);
        const params = new URLSearchParams({
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName
        });
        if (values.day) {
            params.set('day', values.day);
        }
        const response = await fetch(`/api/reports/schedule-today?${params.toString()}`);
        const text = await response.text();
        let payload;
        try {
            payload = text ? JSON.parse(text) : null;
        } catch {
            payload = text;
        }
        showResult('reports-result', response.ok, payload ?? 'لا توجد بيانات');
    });

    attendanceForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        const values = serializeForm(attendanceForm);
        const params = new URLSearchParams({
            universityName: values.universityName,
            collegeName: values.collegeName,
            departmentName: values.departmentName
        });
        if (values.startDate) {
            params.set('startDate', values.startDate);
        }
        if (values.endDate) {
            params.set('endDate', values.endDate);
        }
        const response = await fetch(`/api/reports/attendance-summary?${params.toString()}`);
        const text = await response.text();
        let payload;
        try {
            payload = text ? JSON.parse(text) : null;
        } catch {
            payload = text;
        }
        showResult('reports-result', response.ok, payload ?? 'لا توجد بيانات');
    });
}

function init() {
    attachEntityForm('form-university', 'university');
    attachEntityForm('form-college', 'college');
    attachEntityForm('form-department', 'department');
    attachEntityForm('form-stage', 'stage');
    attachEntityForm('form-group', 'group');
    attachEntityForm('form-course', 'course');
    attachEntityForm('form-instructor', 'instructor');
    attachEntityForm('form-room', 'room');
    attachScheduleForms();
    attachImportForms();
    attachReportForms();
}

document.addEventListener('DOMContentLoaded', init);
