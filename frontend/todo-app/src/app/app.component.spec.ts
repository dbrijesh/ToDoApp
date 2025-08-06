import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { AppComponent } from './app.component';
import { MsalService, MsalBroadcastService, MSAL_GUARD_CONFIG } from '@azure/msal-angular';
import { InteractionStatus, AccountInfo } from '@azure/msal-browser';
import { Subject, of } from 'rxjs';

describe('AppComponent', () => {
  let component: AppComponent;
  let fixture: ComponentFixture<AppComponent>;
  let mockMsalService: jasmine.SpyObj<MsalService>;
  let mockMsalBroadcastService: jasmine.SpyObj<MsalBroadcastService>;
  let httpMock: HttpTestingController;
  let interactionStatusSubject: Subject<InteractionStatus>;

  const mockAccount: Partial<AccountInfo> = {
    localAccountId: 'test-user-id',
    name: 'Test User',
    username: 'test@example.com'
  };

  const mockGuardConfig = {
    interactionType: 0,
    authRequest: {
      scopes: ['user.read', 'openid', 'profile']
    }
  };

  beforeEach(async () => {
    interactionStatusSubject = new Subject<InteractionStatus>();
    
    const msalInstanceSpy = jasmine.createSpyObj('IPublicClientApplication', [
      'getAllAccounts',
      'getActiveAccount',
      'setActiveAccount',
      'initialize',
      'handleRedirectPromise',
      'clearCache'
    ]);
    
    // Setup default return values for MSAL promises
    msalInstanceSpy.initialize.and.returnValue(Promise.resolve());
    msalInstanceSpy.handleRedirectPromise.and.returnValue(Promise.resolve(null));
    msalInstanceSpy.clearCache.and.returnValue(Promise.resolve());

    const msalServiceSpy = jasmine.createSpyObj('MsalService', [
      'loginRedirect', 
      'logoutRedirect', 
      'acquireTokenSilent'
    ], {
      instance: msalInstanceSpy
    });
    
    // Setup acquireTokenSilent to return an observable
    msalServiceSpy.acquireTokenSilent.and.returnValue(of({ 
      accessToken: 'mock-access-token',
      account: mockAccount 
    }));

    const msalBroadcastServiceSpy = jasmine.createSpyObj('MsalBroadcastService', [], {
      inProgress$: interactionStatusSubject.asObservable(),
      msalSubject$: new Subject().asObservable()
    });

    const routerSpy = jasmine.createSpyObj('Router', ['navigate'], {
      url: '/'
    });

    await TestBed.configureTestingModule({
      imports: [AppComponent, HttpClientTestingModule, FormsModule],
      providers: [
        { provide: MsalService, useValue: msalServiceSpy },
        { provide: MsalBroadcastService, useValue: msalBroadcastServiceSpy },
        { provide: MSAL_GUARD_CONFIG, useValue: mockGuardConfig },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
    mockMsalService = TestBed.inject(MsalService) as jasmine.SpyObj<MsalService>;
    mockMsalBroadcastService = TestBed.inject(MsalBroadcastService) as jasmine.SpyObj<MsalBroadcastService>;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the app', () => {
    expect(component).toBeTruthy();
  });

  it('should have correct title', () => {
    expect(component.title).toEqual('TODO App');
  });

  it('should initialize with correct default values', () => {
    expect(component.loginDisplay).toBeFalsy();
    expect(component.userProfile).toBeNull();
    expect(component.todos).toEqual([]);
    expect(component.newTodo).toEqual({ title: '', description: '' });
    expect(component.editingTodo).toBeNull();
  });

  describe('Authentication', () => {
    it('should display login button when not logged in', () => {
      (mockMsalService.instance.getAllAccounts as jasmine.Spy).and.returnValue([]);
      component.ngOnInit();
      fixture.detectChanges();

      const loginButton = fixture.debugElement.query(By.css('.login-btn'));
      expect(loginButton).toBeTruthy();
      expect(loginButton.nativeElement.textContent.trim()).toBe('Sign in with Azure');
    });

    it('should display user info when logged in', () => {
      (mockMsalService.instance.getAllAccounts as jasmine.Spy).and.returnValue([mockAccount as AccountInfo]);
      (mockMsalService.instance.getActiveAccount as jasmine.Spy).and.returnValue(mockAccount as AccountInfo);
      
      component.ngOnInit();
      interactionStatusSubject.next(InteractionStatus.None);
      fixture.detectChanges();

      const userInfo = fixture.debugElement.query(By.css('.user-info'));
      expect(userInfo).toBeTruthy();
      
      const userName = fixture.debugElement.query(By.css('.user-name'));
      const userEmail = fixture.debugElement.query(By.css('.user-email'));
      expect(userName.nativeElement.textContent).toBe('Test User');
      expect(userEmail.nativeElement.textContent).toBe('test@example.com');
    });

    it('should call loginRedirect when login button is clicked', () => {
      (mockMsalService.instance.getAllAccounts as jasmine.Spy).and.returnValue([]);
      component.ngOnInit();
      fixture.detectChanges();

      const loginButton = fixture.debugElement.query(By.css('.login-btn'));
      loginButton.nativeElement.click();

      expect(mockMsalService.loginRedirect).toHaveBeenCalled();
    });

    it('should call logoutRedirect when logout button is clicked', () => {
      (mockMsalService.instance.getAllAccounts as jasmine.Spy).and.returnValue([mockAccount as AccountInfo]);
      (mockMsalService.instance.getActiveAccount as jasmine.Spy).and.returnValue(mockAccount as AccountInfo);
      
      component.ngOnInit();
      interactionStatusSubject.next(InteractionStatus.None);
      fixture.detectChanges();

      const logoutButton = fixture.debugElement.query(By.css('.logout-btn'));
      logoutButton.nativeElement.click();

      expect(mockMsalService.logoutRedirect).toHaveBeenCalledWith({
        postLogoutRedirectUri: 'http://localhost:4200'
      });
    });
  });

  describe('TODO Management', () => {
    beforeEach(() => {
      (mockMsalService.instance.getAllAccounts as jasmine.Spy).and.returnValue([mockAccount as AccountInfo]);
      (mockMsalService.instance.getActiveAccount as jasmine.Spy).and.returnValue(mockAccount as AccountInfo);
      component.ngOnInit();
      interactionStatusSubject.next(InteractionStatus.None);
    });

    it('should load todos on login', () => {
      const mockTodos = [
        { id: 1, title: 'Test Todo', description: 'Test Description', isCompleted: false, createdDate: new Date(), updatedDate: new Date() }
      ];

      const req = httpMock.expectOne('http://localhost:5000/api/todos');
      expect(req.request.method).toBe('GET');
      req.flush(mockTodos);

      expect(component.todos).toEqual(mockTodos);
    });

    it('should add new todo', () => {
      component.newTodo = { title: 'New Todo', description: 'New Description' };
      
      const mockCreatedTodo = {
        id: 1,
        title: 'New Todo',
        description: 'New Description',
        isCompleted: false,
        createdDate: new Date(),
        updatedDate: new Date()
      };

      component.addTodo();

      const req = httpMock.expectOne('http://localhost:5000/api/todos');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        title: 'New Todo',
        description: 'New Description',
        isCompleted: false
      });
      req.flush(mockCreatedTodo);

      expect(component.todos).toContain(mockCreatedTodo);
      expect(component.newTodo).toEqual({ title: '', description: '' });
    });

    it('should not add todo with empty title', () => {
      component.newTodo = { title: '', description: 'Description' };
      component.addTodo();
      httpMock.expectNone('http://localhost:5000/api/todos');
    });

    it('should enter edit mode', () => {
      const mockTodo = {
        id: 1,
        title: 'Test Todo',
        description: 'Test Description',
        isCompleted: false,
        createdDate: new Date(),
        updatedDate: new Date()
      };

      component.editTodo(mockTodo);
      expect(component.editingTodo).toEqual(mockTodo);
    });

    it('should update todo', () => {
      const mockTodo = {
        id: 1,
        title: 'Updated Todo',
        description: 'Updated Description',
        isCompleted: false,
        createdDate: new Date(),
        updatedDate: new Date()
      };

      component.todos = [mockTodo];
      component.editingTodo = { ...mockTodo };
      component.editingTodo.title = 'Updated Title';

      component.updateTodo();

      const req = httpMock.expectOne('http://localhost:5000/api/todos/1');
      expect(req.request.method).toBe('PUT');
      req.flush({ ...component.editingTodo, updatedDate: new Date() });

      expect(component.editingTodo).toBeNull();
    });

    it('should delete todo', () => {
      const mockTodo = {
        id: 1,
        title: 'Test Todo',
        description: 'Test Description',
        isCompleted: false,
        createdDate: new Date(),
        updatedDate: new Date()
      };

      component.todos = [mockTodo];
      component.deleteTodo(1);

      const req = httpMock.expectOne('http://localhost:5000/api/todos/1');
      expect(req.request.method).toBe('DELETE');
      req.flush({});

      expect(component.todos).not.toContain(mockTodo);
    });

    it('should toggle todo completion', () => {
      const mockTodo = {
        id: 1,
        title: 'Test Todo',
        description: 'Test Description',
        isCompleted: false,
        createdDate: new Date(),
        updatedDate: new Date()
      };

      component.todos = [mockTodo];
      component.toggleComplete(mockTodo);

      expect(mockTodo.isCompleted).toBeTruthy();

      const req = httpMock.expectOne('http://localhost:5000/api/todos/1');
      expect(req.request.method).toBe('PUT');
      req.flush({ ...mockTodo, isCompleted: true });
    });

    it('should cancel edit', () => {
      component.editingTodo = {
        id: 1,
        title: 'Test Todo',
        description: 'Test Description',
        isCompleted: false,
        createdDate: new Date(),
        updatedDate: new Date()
      };

      component.cancelEdit();
      expect(component.editingTodo).toBeNull();
    });
  });

  describe('UI Rendering', () => {
    it('should show empty state when no todos', () => {
      (mockMsalService.instance.getAllAccounts as jasmine.Spy).and.returnValue([mockAccount as AccountInfo]);
      component.ngOnInit();
      interactionStatusSubject.next(InteractionStatus.None);
      fixture.detectChanges();

      httpMock.expectOne('http://localhost:5000/api/todos').flush([]);
      fixture.detectChanges();

      const emptyState = fixture.debugElement.query(By.css('.empty-state'));
      expect(emptyState).toBeTruthy();
      expect(emptyState.nativeElement.textContent).toContain('No TODOs yet');
    });

    it('should disable add button when title is empty', () => {
      (mockMsalService.instance.getAllAccounts as jasmine.Spy).and.returnValue([mockAccount as AccountInfo]);
      component.ngOnInit();
      interactionStatusSubject.next(InteractionStatus.None);
      fixture.detectChanges();

      httpMock.expectOne('http://localhost:5000/api/todos').flush([]);
      
      component.newTodo.title = '';
      fixture.detectChanges();

      const addButton = fixture.debugElement.query(By.css('.add-btn'));
      expect(addButton.nativeElement.disabled).toBeTruthy();
    });

    it('should enable add button when title is provided', () => {
      (mockMsalService.instance.getAllAccounts as jasmine.Spy).and.returnValue([mockAccount as AccountInfo]);
      component.ngOnInit();
      interactionStatusSubject.next(InteractionStatus.None);
      fixture.detectChanges();

      httpMock.expectOne('http://localhost:5000/api/todos').flush([]);
      
      component.newTodo.title = 'New Todo';
      fixture.detectChanges();

      const addButton = fixture.debugElement.query(By.css('.add-btn'));
      expect(addButton.nativeElement.disabled).toBeFalsy();
    });
  });

  describe('Error Handling', () => {
    it('should handle todo loading error', () => {
      spyOn(console, 'error');
      (mockMsalService.instance.getAllAccounts as jasmine.Spy).and.returnValue([mockAccount as AccountInfo]);
      component.ngOnInit();
      interactionStatusSubject.next(InteractionStatus.None);

      const req = httpMock.expectOne('http://localhost:5000/api/todos');
      req.error(new ProgressEvent('Network error'));

      expect(console.error).toHaveBeenCalled();
    });

    it('should handle todo creation error', () => {
      spyOn(console, 'error');
      component.newTodo = { title: 'New Todo', description: 'Description' };
      component.addTodo();

      const req = httpMock.expectOne('http://localhost:5000/api/todos');
      req.error(new ProgressEvent('Network error'));

      expect(console.error).toHaveBeenCalled();
    });
  });
});
