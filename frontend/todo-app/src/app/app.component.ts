import { Component, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Amplify } from 'aws-amplify';
import { getCurrentUser, signInWithRedirect, signOut, fetchAuthSession } from 'aws-amplify/auth';
import { amplifyConfig } from '../amplify-config';
import { Subject, takeUntil } from 'rxjs';

interface TodoItem {
  id: number;
  title: string;
  description: string;
  isCompleted: boolean;
  createdDate: Date;
  updatedDate: Date;
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'TODO App';
  loginDisplay = false;
  userProfile: any = null;
  loginInProgress = false;
  isInitializing = true;
  isIframe = false;
  private readonly _destroying$ = new Subject<void>();

  todos: TodoItem[] = [];
  newTodo = { title: '', description: '' };
  editingTodo: TodoItem | null = null;
  private apiUrl = 'http://localhost:5000/api/todos';

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // Configure Amplify
    Amplify.configure(amplifyConfig);
  }

  /**
   * Component initialization - sets up Amplify authentication
   */
  async ngOnInit() {
    this.isIframe = window !== window.parent && !window.opener;
    
    try {
      // Check if user is already authenticated
      const user = await getCurrentUser();
      console.log('Current user:', user);
      
      if (user) {
        this.userProfile = {
          name: user.username,
          email: user.signInDetails?.loginId || user.username
        };
        this.loginDisplay = true;
        this.loadTodos();
        console.log('User authenticated successfully');
      }
    } catch (error) {
      // User is not authenticated, which is fine
      console.log('User not authenticated:', error);
    } finally {
      this.isInitializing = false;
    }
  }

  /**
   * Initiates SAML authentication through Cognito
   */
  async login() {
    if (this.loginInProgress) {
      return;
    }

    this.loginInProgress = true;

    try {
      // Redirect to Azure SAML through Cognito federation
      await signInWithRedirect({ provider: { custom: 'AzureSAML' } });
    } catch (error) {
      console.error('Login error:', error);
      this.loginInProgress = false;
    }
  }

  /**
   * Initiates NAM-SAML IDP-initiated authentication
   */
  async loginWithNAM() {
    if (this.loginInProgress) {
      return;
    }

    this.loginInProgress = true;

    try {
      // For IDP-initiated login, redirect directly to NAM SSO URL
      window.location.href = 'https://access.webdev.bank.com/cognitosso';
    } catch (error) {
      console.error('NAM login error:', error);
      this.loginInProgress = false;
    }
  }

  /**
   * Logs out the user
   */
  async logout() {
    try {
      await signOut();
      this.loginDisplay = false;
      this.userProfile = null;
      this.todos = [];
      this.router.navigate(['/']);
    } catch (error) {
      console.error('Logout error:', error);
    }
  }

  /**
   * Gets authorization headers with access token for API requests
   */
  private async getAuthHeaders(): Promise<HttpHeaders> {
    try {
      const session = await fetchAuthSession();
      const accessToken = session.tokens?.accessToken?.toString();
      
      if (accessToken) {
        return new HttpHeaders({
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json'
        });
      }
    } catch (error) {
      console.error('Token acquisition failed:', error);
    }
    
    return new HttpHeaders({
      'Content-Type': 'application/json'
    });
  }

  /**
   * Loads all TODOs for the authenticated user from the API
   */
  async loadTodos() {
    try {
      const headers = await this.getAuthHeaders();
      this.http.get<TodoItem[]>(this.apiUrl, { headers })
        .subscribe({
          next: (todos) => this.todos = todos,
          error: (error) => console.error('Error loading todos:', error)
        });
    } catch (error) {
      console.error('Error getting auth headers for loading todos:', error);
    }
  }

  /**
   * Creates a new TODO item
   */
  async addTodo() {
    if (this.newTodo.title.trim()) {
      try {
        const todo = {
          title: this.newTodo.title,
          description: this.newTodo.description,
          isCompleted: false
        };

        const headers = await this.getAuthHeaders();
        this.http.post<TodoItem>(this.apiUrl, todo, { headers })
          .subscribe({
            next: (createdTodo) => {
              this.todos.push(createdTodo);
              this.newTodo = { title: '', description: '' };
            },
            error: (error) => console.error('Error adding todo:', error)
          });
      } catch (error) {
        console.error('Error getting auth headers for adding todo:', error);
      }
    }
  }

  /**
   * Enters edit mode for a TODO item
   */
  editTodo(todo: TodoItem) {
    this.editingTodo = { ...todo };
  }

  /**
   * Updates an existing TODO item
   */
  async updateTodo() {
    if (this.editingTodo) {
      try {
        const headers = await this.getAuthHeaders();
        this.http.put<TodoItem>(`${this.apiUrl}/${this.editingTodo.id}`, this.editingTodo, { headers })
          .subscribe({
            next: (updatedTodo) => {
              const index = this.todos.findIndex(t => t.id === updatedTodo.id);
              if (index !== -1) {
                this.todos[index] = updatedTodo;
              }
              this.editingTodo = null;
            },
            error: (error) => console.error('Error updating todo:', error)
          });
      } catch (error) {
        console.error('Error getting auth headers for updating todo:', error);
      }
    }
  }

  /**
   * Deletes a TODO item
   */
  async deleteTodo(id: number) {
    try {
      const headers = await this.getAuthHeaders();
      this.http.delete(`${this.apiUrl}/${id}`, { headers })
        .subscribe({
          next: () => {
            this.todos = this.todos.filter(t => t.id !== id);
          },
          error: (error) => console.error('Error deleting todo:', error)
        });
    } catch (error) {
      console.error('Error getting auth headers for deleting todo:', error);
    }
  }

  /**
   * Toggles the completion status of a TODO item
   */
  async toggleComplete(todo: TodoItem) {
    try {
      todo.isCompleted = !todo.isCompleted;
      const headers = await this.getAuthHeaders();
      this.http.put<TodoItem>(`${this.apiUrl}/${todo.id}`, todo, { headers })
        .subscribe({
          next: (updatedTodo) => {
            const index = this.todos.findIndex(t => t.id === updatedTodo.id);
            if (index !== -1) {
              this.todos[index] = updatedTodo;
            }
          },
          error: (error) => console.error('Error updating todo:', error)
        });
    } catch (error) {
      console.error('Error getting auth headers for toggling todo:', error);
    }
  }

  /**
   * Cancels the current edit operation
   */
  cancelEdit() {
    this.editingTodo = null;
  }

  /**
   * Component cleanup - unsubscribes from all observables
   */
  ngOnDestroy(): void {
    this._destroying$.next(undefined);
    this._destroying$.complete();
  }
}
