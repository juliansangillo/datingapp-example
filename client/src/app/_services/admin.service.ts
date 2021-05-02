import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Photo } from '../_models/photo';
import { User } from '../_models/user';

@Injectable({
    providedIn: 'root'
})
export class AdminService {
    baseUrl = environment.apiUrl;

    constructor(private http: HttpClient) { }

    getUsersWithRoles() {
        return this.http.get<Partial<User[]>>(this.baseUrl + '/admin/users-with-roles');
    }

    updateUserRoles(username: string, roles: string[]) {
        return this.http.post(this.baseUrl + '/admin/edit-roles/' + username + '?roles=' + roles, {});
    }

    getPhotosToModerate() {
        return this.http.get<Photo[]>(this.baseUrl + '/admin/photos-to-moderate');
    }

    approvePhoto(id: number) {
        return this.http.put(this.baseUrl + '/admin/photo-for-approval/' + id, {});
    }

    rejectPhoto(id: number) {
        return this.http.delete(this.baseUrl + '/admin/photo-for-approval/' + id, {});
    }
}
